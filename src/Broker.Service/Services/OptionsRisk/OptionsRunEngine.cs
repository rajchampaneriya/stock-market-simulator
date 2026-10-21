using MarketSimulator;
using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Market;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using OptionsRisk.Core.Simulation;

namespace Broker.Service.Services.OptionsRisk;

/// <summary>
/// Host adapter for one educational options run: drives the existing
/// RealisticPriceEngine (own seeded instance, NIFTY profile at index level),
/// overlays event-day spikes with IV expansion/decay, and streams snapshots.
/// Play / pause / speed and decisions arrive via the controller.
/// </summary>
public sealed class OptionsRunEngine : BackgroundService
{
    private readonly OptionsRiskSettings _settings;
    private readonly OptionsStreamManager _stream;
    private readonly ILogger<OptionsRunEngine> _logger;
    private readonly object _gate = new();

    private RealisticPriceEngine _priceEngine = new();
    private SpikeGenerator _spikes = new(0, 0.01, 0.03, 0.3);
    private EventShockOverlay _eventShock = new(0.995);
    private IntradayPriceNormalizer _normalizer = new(25000, 0.13, 375, 1);
    private StrategyRun? _run;
    private double _ivMultiplier = 1.0;
    private int _simMinute;
    private int _speed = 1;
    private string _symbol = "OPTIONS-NIFTY";

    public OptionsRunEngine(OptionsRiskSettings settings, OptionsStreamManager stream, ILogger<OptionsRunEngine> logger)
    {
        _settings = settings;
        _stream = stream;
        _logger = logger;
    }

    // --- Control surface (called from the API controller) ---

    public OptionsSnapshotDto Start(int? seed)
    {
        lock (_gate)
        {
            var config = _settings.ToStrategyConfig(seed);
            _priceEngine = new RealisticPriceEngine(config.Seed);
            _priceEngine.RegisterSymbol(new SymbolProfile
            {
                Symbol = _symbol,
                BasePrice = (decimal)config.StartingSpot,
                AnnualisedVolatility = config.UnderlyingAnnualVol,
                MeanReversionStrength = config.MeanReversionStrength,
                BaseVolume = 6000,
            });
            _eventShock = new EventShockOverlay(config.SpikeRetentionPerTick);
            _normalizer = new IntradayPriceNormalizer(config.StartingSpot,
                config.UnderlyingAnnualVol, config.TradingMinutesPerDay,
                config.SimMinutesPerTick, config.MeanReversionStrength);
            _spikes = new SpikeGenerator(
                config.SpikeProbabilityPerTick, config.SpikeMinMagnitude,
                config.SpikeMaxMagnitude, config.SpikeIvJumpPerPercent,
                seed: config.Seed + 1);
            _run = new StrategyRun(config, new BlackScholesPricer(config.RiskFreeRate), DefaultRules());
            _ivMultiplier = 1.0;
            _simMinute = 0;
            _speed = 1;

            var snapshot = _run.Open(config.StartingSpot);
            var dto = OptionsDtoMapper.ToDto(snapshot, _speed, config.LotSize);
            _stream.Broadcast(dto);
            _logger.LogInformation("Options risk run started: spot {Spot}, seed {Seed}", config.StartingSpot, config.Seed);
            return dto;
        }
    }

    private static ICoachRule[] DefaultRules() =>
    [
        new DeltaHedgeRule(),
        new LegStopLossRule(),
        new StrikeBreachRule(),
        new IvSpikeRule(),
        new DrawdownRule(),
    ];

    public OptionsSnapshotDto? Pause()
    {
        lock (_gate)
        {
            _run?.Pause();
            return CurrentDto();
        }
    }

    public OptionsSnapshotDto? Resume()
    {
        lock (_gate)
        {
            _userPaused = false;
            _run?.Resume();
            return CurrentDto();
        }
    }

    public OptionsSnapshotDto? SetSpeed(int speed)
    {
        lock (_gate)
        {
            if (_settings.EffectiveSpeeds.Contains(speed))
                _speed = speed;
            return CurrentDto();
        }
    }

    public OptionsSnapshotDto? ApplyAction(ApplyActionRequest request)
    {
        lock (_gate)
        {
            if (_run == null) return null;

            var suggestion = _run.LastSnapshot?.Suggestions.FirstOrDefault(s => s.Id == request.SuggestionId);
            if (suggestion == null) return CurrentDto();

            var action = suggestion.Action;
            if (action.Kind == CoachActionKind.ExitLeg)
                _run.ExitLeg(action.LegId!, suggestion.Rule, suggestion.Title);
            else
                _run.HedgeFutures(
                    action.Kind == CoachActionKind.BuyFutures ? action.Lots : -action.Lots,
                    suggestion.Rule, suggestion.Title);

            var dto = CurrentDto()!;
            _stream.Broadcast(dto);
            return dto;
        }
    }

    public OptionsSnapshotDto? IgnoreAction(string suggestionId)
    {
        lock (_gate)
        {
            if (_run == null) return null;
            var suggestion = _run.LastSnapshot?.Suggestions.FirstOrDefault(s => s.Id == suggestionId);
            if (suggestion == null) return CurrentDto();

            _run.IgnoreSuggestion(suggestion);
            var dto = CurrentDto()!;
            _stream.Broadcast(dto);
            return dto;
        }
    }

    public OptionsSnapshotDto? CurrentDto()
    {
        lock (_gate)
        {
            return _run?.LastSnapshot == null
                ? null
                : OptionsDtoMapper.ToDto(_run.LastSnapshot, _speed, _settings.LotSize);
        }
    }

    public sealed record ApplyActionRequest(string SuggestionId);

    public MonteCarloComparison? ProjectMonteCarlo(int signedLots, MonteCarloSettings settings)
    {
        // Snapshot consistency: no market tick or user action may mutate the
        // positions while a counterfactual projection reads them.
        lock (_gate)
            return _run is null ? null : MonteCarloProjector.Compare(_run, signedLots, settings);
    }

    // --- Tick loop ---

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Options run engine host loop ready (feature-gated service)");

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMilliseconds(Math.Max(25, _settings.TickIntervalMs / (double)_speed));
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            OptionsSnapshotDto? dto = null;
            lock (_gate)
            {
                if (_run is not { Status: RunStatus.Running }) continue;
                dto = TickOnce();
            }

            if (dto != null)
                _stream.Broadcast(dto);
        }
    }

    private OptionsSnapshotDto? TickOnce()
    {
        var config = _run!.Config;
        var (walkPriceDecimal, _) = _priceEngine.NextTick(_symbol);
        var walkPrice = _normalizer.Next((double)walkPriceDecimal);

        // Retain the shock in log-price space: otherwise the next base-engine
        // tick silently erases the jump and creates an unlabelled reverse spike.
        var spike = _spikes.NextSpike();
        var beforeShock = _eventShock.BeforeSpike(walkPrice);
        var spot = _eventShock.Next(walkPrice, spike?.MagnitudeFraction);
        SpikeEvent? spikeEvent = null;
        if (spike is { } s)
        {
            _ivMultiplier = Math.Min(config.IvMultiplierCap, _ivMultiplier + s.IvMultiplierJump);
            spikeEvent = new SpikeEvent(0, _simMinute, beforeShock, spot, s.IvMultiplierJump);
        }

        // Vol expansion partly reverts every tick after the spike.
        _ivMultiplier = 1.0 + (_ivMultiplier - 1.0) * config.IvDecayPerTick;
        _simMinute += (int)config.SimMinutesPerTick;

        var snapshot = _run.OnTick(new MarketTick(spot, _ivMultiplier, _simMinute, spikeEvent));

        // A spike demands a decision: freeze the tape if configured to.
        if (spikeEvent != null && config.AutoPauseOnSpike && snapshot.Status == RunStatus.Running)
        {
            snapshot = _run.Pause();
            _logger.LogInformation("Auto-paused after spike: spot {From} → {To}", spikeEvent.FromSpot, spikeEvent.ToSpot);
        }

        return OptionsDtoMapper.ToDto(snapshot, _speed, config.LotSize);
    }
}
