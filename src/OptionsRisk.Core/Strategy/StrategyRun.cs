using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Costs;
using OptionsRisk.Core.Margin;
using OptionsRisk.Core.Pricing;

namespace OptionsRisk.Core.Strategy;

/// <summary>
/// Pure state machine for one educational short-straddle + short-strangle run.
/// The host pushes MarketTicks in; the run reprices, updates P/L, evaluates
/// coach rules and emits an immutable snapshot. No timers, no I/O.
/// </summary>
public sealed class StrategyRun
{
    private readonly StrategyConfig _config;
    private readonly IOptionPricer _pricer;
    private readonly VolatilitySurface _surface;
    private readonly IReadOnlyList<ICoachRule> _rules;
    private readonly ExecutionAccounting _accounting = new();
    private readonly IndianFeeSchedule _fees = new();

    private readonly List<OptionLeg> _legs = new();
    private readonly List<FuturesPosition> _futures = new();
    private readonly List<(int Tick, double Spot)> _spotHistory = new();
    private readonly List<SpikeEvent> _spikes = new();
    private readonly List<DecisionRecord> _decisions = new();

    // Suggestion lifecycle: id → (lastRaisedTick, ignoredUntilTick)
    private readonly Dictionary<string, (int Raised, int IgnoredUntil)> _suggestionState = new();
    private List<CoachSuggestion> _lastSuggestions = [];

    private const int SpotHistoryCap = 900;
    private const int DecisionsCap = 200;
    private const int PayoffGridPoints = 61;
    private const double PayoffRange = 1500;

    public RunStatus Status { get; private set; } = RunStatus.NotStarted;
    public int TickIndex { get; private set; }
    public int SimMinute { get; private set; }
    public double Spot { get; private set; }
    public double IvMultiplier { get; private set; } = 1.0;
    public double AtmStrike { get; private set; }
    public double MinutesToExpiry { get; private set; }
    public double PeakNetPnl { get; private set; }
    public double MaxLossSoFar { get; private set; }
    public RunSnapshot? LastSnapshot { get; private set; }

    public StrategyRun(StrategyConfig config, IOptionPricer pricer, IEnumerable<ICoachRule>? rules = null)
    {
        _config = config;
        _pricer = pricer;
        _surface = new VolatilitySurface(config.BaseIv, config.SkewSlope);
        _rules = rules?.ToList() ?? [];
    }

    public IReadOnlyList<OptionLeg> Legs => _legs;
    public IReadOnlyList<FuturesPosition> Futures => _futures;
    public IReadOnlyList<DecisionRecord> Decisions => _decisions;
    public StrategyConfig Config => _config;

    // --- Lifecycle ---

    /// <summary>Round spot to nearest strike, then sell 1 lot of each leg:
    /// ATM CE + ATM PE (straddle) and (ATM+off) CE + (ATM−off) PE (strangle).</summary>
    public RunSnapshot Open(double spot)
    {
        if (Status != RunStatus.NotStarted)
            throw new InvalidOperationException("Run already opened.");

        Spot = spot;
        AtmStrike = Math.Round(spot / _config.StrikeStep, MidpointRounding.AwayFromZero) * _config.StrikeStep;
        MinutesToExpiry = _config.DaysToExpiry * _config.TradingMinutesPerDay;

        foreach (var (strike, type) in new[]
        {
            (AtmStrike, OptionType.Call), (AtmStrike, OptionType.Put),
            (AtmStrike + _config.WingOffset, OptionType.Call),
            (AtmStrike - _config.WingOffset, OptionType.Put),
        })
        {
            var iv = _surface.IvFor(type, Spot, strike, IvMultiplier);
            var quote = _pricer.Price(type, Spot, strike, YearsToExpiry(), iv);
            _legs.Add(new OptionLeg
            {
                Id = $"{strike:0}-{(type == OptionType.Call ? "CE" : "PE")}",
                Strike = strike,
                Type = type,
                Direction = (int)LegSide.Short,
                QuantityLots = 1,
                EntryPrice = SimulatedFill.Price(quote.Price, TradeSide.Sell,
                    _config.OptionTickSize, _config.NormalOptionSlippageTicks),
                EntryIv = iv,
                Quote = quote,
            });
        }

        foreach (var leg in _legs)
            _accounting.Record(IndianDerivativesCosts.Calculate(DerivativeKind.Option, TradeSide.Sell,
                leg.EntryPrice, leg.Quote.Price, leg.QuantityLots * _config.LotSize, _fees));
        if (_config.EnforceEstimatedMargin && EstimateMargin().MarginShortfall)
            throw new InvalidOperationException("Insufficient virtual capital for estimated opening margin.");
        _decisions.Add(new DecisionRecord(0, SimMinute, "Strategy", DescribeStrategy(), "opened"));
        Status = RunStatus.Running;
        TickIndex = 0;
        _spotHistory.Add((0, Spot));

        return BuildSnapshot([]);
    }

    public RunSnapshot Pause()
    {
        if (Status == RunStatus.Running) Status = RunStatus.Paused;
        return BuildSnapshot(_lastSuggestions);
    }

    public RunSnapshot Resume()
    {
        if (Status == RunStatus.Paused) Status = RunStatus.Running;
        return BuildSnapshot(_lastSuggestions);
    }

    /// <summary>Advance the market by one tick and reprice everything.</summary>
    public RunSnapshot OnTick(MarketTick tick)
    {
        if (Status is not (RunStatus.Running or RunStatus.Paused))
            throw new InvalidOperationException($"Cannot tick in status {Status}.");

        TickIndex++;
        SimMinute = tick.SimMinute;
        Spot = tick.Spot;
        IvMultiplier = Math.Min(tick.IvMultiplier, _config.IvMultiplierCap);

        if (tick.Spike is { } spike)
            _spikes.Add(spike with { TickIndex = TickIndex });

        MinutesToExpiry = Math.Max(0, MinutesToExpiry - _config.SimMinutesPerTick);

        RepriceOpenLegs();

        if (MinutesToExpiry <= 0)
            SettleAtExpiry();

        _spotHistory.Add((TickIndex, Spot));
        if (_spotHistory.Count > SpotHistoryCap)
            _spotHistory.RemoveAt(0);

        UpdateDecisionOutcomes();
        UpdateDrawdownTrackers();

        var suggestions = Status == RunStatus.Running
            ? EvaluateCoach()
            : PruneStaleSuggestions([]);

        _lastSuggestions = suggestions;

        var snapshot = BuildSnapshot(suggestions);
        LastSnapshot = snapshot;
        return snapshot;
    }

    // --- User actions ---

    /// <summary>Buy (positive) or sell (negative) futures lots as a delta hedge.</summary>
    public RunSnapshot HedgeFutures(int signedLots, string rule, string description)
    {
        if (signedLots == 0) return LastSnapshot ?? BuildSnapshot([]);

        var reference = CurrentFuturesPrice();
        var side = signedLots > 0 ? TradeSide.Buy : TradeSide.Sell;
        var entry = SimulatedFill.Price(reference, side, _config.FuturesTickSize,
            _config.FuturesSlippageTicks);
        var charge = IndianDerivativesCosts.Calculate(DerivativeKind.Future, side, entry,
            reference, Math.Abs(signedLots) * _config.LotSize, _fees);
        _futures.Add(new FuturesPosition
        {
            Id = $"FUT-{_futures.Count + 1}",
            Direction = Math.Sign(signedLots),
            QuantityLots = Math.Abs(signedLots),
            EntryPrice = entry,
        });
        _futures[^1].Mark(reference);
        if (_config.EnforceEstimatedMargin &&
            EstimateMargin().Required > _config.Capital + NetPnlNow() - (charge.Total - charge.Slippage))
        {
            _futures.RemoveAt(_futures.Count - 1);
            throw new InvalidOperationException("Hedge would exceed estimated virtual margin. Close exposure first.");
        }
        _accounting.Record(charge);

        RecordDecision(rule, description, "applied");
        return BuildSnapshot(EvaluateCoach());
    }

    /// <summary>Exit one option leg at its last traded price.</summary>
    public RunSnapshot ExitLeg(string legId, string rule, string description)
    {
        var leg = _legs.FirstOrDefault(l => l.Id == legId && l.Status == LegStatus.Open)
            ?? throw new KeyNotFoundException($"Open leg '{legId}' not found.");

        var reference = leg.LastPrice;
        var ticks = IvMultiplier >= _config.IvSpikeWarnMultiplier
            ? _config.SpikeOptionSlippageTicks : _config.NormalOptionSlippageTicks;
        var fill = SimulatedFill.Price(reference, TradeSide.Buy, _config.OptionTickSize, ticks);
        leg.Exit(fill);
        _accounting.Record(IndianDerivativesCosts.Calculate(DerivativeKind.Option, TradeSide.Buy,
            fill, reference, leg.QuantityLots * _config.LotSize, _fees));
        RecordDecision(rule, description, "applied");
        return BuildSnapshot(EvaluateCoach());
    }

    public RunSnapshot IgnoreSuggestion(CoachSuggestion suggestion)
    {
        if (_suggestionState.TryGetValue(suggestion.Id, out var state))
            _suggestionState[suggestion.Id] = (state.Raised, TickIndex + _config.SuggestionCooldownTicks);

        RecordDecision(suggestion.Rule, $"Ignored: {suggestion.Title}", "ignored",
            suggestion.Projection.HoldPnl, suggestion.Projection.ActPnl);
        return BuildSnapshot(EvaluateCoach());
    }

    // --- Internals ---

    private double YearsToExpiry() => MinutesToExpiry / (_config.TradingMinutesPerDay * 252.0);

    private double CurrentFuturesPrice() =>
        FuturesMath.Price(Spot, _config.RiskFreeRate, YearsToExpiry());

    private void RepriceOpenLegs()
    {
        var t = YearsToExpiry();
        foreach (var leg in _legs.Where(l => l.Status == LegStatus.Open))
        {
            var iv = _surface.IvFor(leg.Type, Spot, leg.Strike, IvMultiplier);
            leg.Reprice(_pricer.Price(leg.Type, Spot, leg.Strike, t, iv));
        }

        var futPrice = CurrentFuturesPrice();
        foreach (var f in _futures.Where(f => f.Status == LegStatus.Open))
            f.Mark(futPrice);
    }

    /// <summary>At expiry, open options settle at intrinsic and futures at spot.</summary>
    private void SettleAtExpiry()
    {
        if (Status == RunStatus.Finished) return;

        foreach (var leg in _legs.Where(l => l.Status == LegStatus.Open).ToList())
            leg.Exit(leg.Intrinsic(Spot));

        foreach (var f in _futures.Where(f => f.Status == LegStatus.Open).ToList())
            f.Exit();

        Status = RunStatus.Finished;
        RecordDecision("Expiry", $"Strategy settled at spot {Spot:0}", "expired");
    }

    private void RecordDecision(string rule, string description, string outcome,
        double? holdPnl = null, double? actPnl = null)
    {
        _decisions.Add(new DecisionRecord(
            TickIndex, SimMinute, rule, description, outcome,
            holdPnl, actPnl, NetPnlNow()));

        if (_decisions.Count > DecisionsCap)
            _decisions.RemoveAt(0);
    }

    private double NetPnlNow() =>
        PortfolioMath.RealizedPnl(_legs, _futures, _config.LotSize)
        + PortfolioMath.UnrealizedPnl(_legs, _futures, _config.LotSize)
        - _accounting.Fees;

    private void UpdateDecisionOutcomes()
    {
        for (var i = 0; i < _decisions.Count; i++)
        {
            var d = _decisions[i];
            if (d.Outcome is "applied" or "ignored")
                _decisions[i] = d with { OutcomePnl = Math.Round(NetPnlNow() - d.NetPnlAtDecision) };
        }
    }

    private void UpdateDrawdownTrackers()
    {
        var net = NetPnlNow();
        PeakNetPnl = Math.Max(PeakNetPnl, net);
        MaxLossSoFar = Math.Min(MaxLossSoFar, net);
    }

    private double DrawdownFraction()
    {
        var net = NetPnlNow();
        var dd = PeakNetPnl - net;
        return _config.Capital > 0 ? dd / _config.Capital : 0;
    }

    private List<CoachSuggestion> EvaluateCoach()
    {
        var context = BuildContext();
        var active = new List<CoachSuggestion>();

        foreach (var rule in _rules)
        {
            foreach (var s in rule.Evaluate(context))
            {
                if (_suggestionState.TryGetValue(s.Id, out var state) && TickIndex < state.IgnoredUntil)
                    continue;
                _suggestionState[s.Id] = (TickIndex, 0);
                var existing = active.FirstOrDefault(x => x.Id == s.Id);
                if (existing != null) active.Remove(existing);
                active.Add(s);
            }
        }

        var suggestions = PruneStaleSuggestions(active);
        _lastSuggestions = suggestions;
        return suggestions;
    }

    private List<CoachSuggestion> PruneStaleSuggestions(List<CoachSuggestion> active)
    {
        foreach (var id in _suggestionState.Keys.Where(id => active.All(s => s.Id != id)).ToList())
            _suggestionState.Remove(id);
        return active;
    }

    private CoachContext BuildContext()
    {
        var totals = BuildTotals();
        return new CoachContext(
            _config, Spot, IvMultiplier, CurrentFuturesPrice(), MinutesToExpiry, AtmStrike,
            _legs, _futures, totals, ComputeBreakevens(), TickIndex);
    }

    private PortfolioTotals BuildTotals()
    {
        var realized = PortfolioMath.RealizedPnl(_legs, _futures, _config.LotSize);
        var unrealized = PortfolioMath.UnrealizedPnl(_legs, _futures, _config.LotSize);
        return new PortfolioTotals
        {
            Realized = Math.Round(realized),
            Costs = _accounting.Snapshot(),
            Margin = EstimateMargin(),
            Unrealized = Math.Round(unrealized),
            PremiumCollected = Math.Round(PortfolioMath.PremiumCollected(_legs, _config.LotSize)),
            MaxLossSoFar = Math.Round(Math.Min(MaxLossSoFar, NetPnlNow())),
            DrawdownFraction = Math.Round(DrawdownFraction(), 4),
            NetGreeks = PortfolioMath.NetGreeks(_legs, _futures, _config.LotSize, _config.RiskFreeRate, YearsToExpiry()),
        };
    }

    /// <summary>Spot levels where the expiry P/L curve crosses zero.</summary>
    private List<double> ComputeBreakevens()
    {
        var curve = ExpiryCurve();
        var breakevens = new List<double>();
        for (var i = 1; i < curve.Count; i++)
        {
            var (s0, p0) = curve[i - 1];
            var (s1, p1) = curve[i];
            if (p0 == 0 || p1 == 0 || Math.Sign(p0) != Math.Sign(p1))
            {
                var t = Math.Abs(p0) / (Math.Abs(p0) + Math.Abs(p1));
                breakevens.Add(Math.Round(s0 + t * (s1 - s0)));
            }
        }
        return breakevens;
    }

    private List<PayoffPoint> ExpiryCurve() => BuildCurve(s => Math.Round(
        PortfolioMath.ExpiryPnl(_legs, _futures, _config.LotSize, s) - _accounting.Fees));

    private List<PayoffPoint> TodayCurve() =>
        BuildCurve(s => Math.Round(MarkPnlAt(s, IvMultiplier, YearsToExpiry()) - _accounting.Fees));

    /// <summary>Reprice the same open book under a counterfactual spot/IV state.</summary>
    private double MarkPnlAt(double spot, double ivMultiplier, double years)
    {
        double pnl = 0;
        foreach (var leg in _legs)
        {
            var units = Math.Abs(leg.QuantityLots) * _config.LotSize;
            if (leg.Status != LegStatus.Open)
            {
                pnl += leg.RealizedPoints * units;
                continue;
            }
            var iv = _surface.IvFor(leg.Type, spot, leg.Strike, ivMultiplier);
            var price = _pricer.Price(leg.Type, spot, leg.Strike, years, iv).Price;
            pnl += leg.Direction * (price - leg.EntryPrice) * units;
        }
        var futPx = FuturesMath.Price(spot, _config.RiskFreeRate, years);
        foreach (var f in _futures)
        {
            var units = Math.Abs(f.QuantityLots) * _config.LotSize;
            pnl += (f.Status == LegStatus.Open
                ? f.Direction * (futPx - f.EntryPrice)
                : f.RealizedPoints) * units;
        }
        return pnl;
    }

    private MarginEstimate EstimateMargin()
    {
        var t = YearsToExpiry();
        var current = MarkPnlAt(Spot, IvMultiplier, t);
        var shock = _config.MarginStressSpotFraction;
        var stressedIv = Math.Min(_config.IvMultiplierCap,
            IvMultiplier * _config.MarginStressIvMultiplier);
        var scenarios = new List<MarginScenario>();
        foreach (var move in new[] { -shock, -shock / 2, shock / 2, shock })
        {
            var stressedSpot = Spot * (1 + move);
            var pnlChange = MarkPnlAt(stressedSpot, stressedIv, t) - current;
            scenarios.Add(new MarginScenario(move,
                (stressedIv - IvMultiplier) * _config.BaseIv * 100, pnlChange));
        }
        var shorts = _legs.Where(l => l.Status == LegStatus.Open && l.Direction < 0)
            .Sum(l => l.QuantityLots);
        var futures = _futures.Where(f => f.Status == LegStatus.Open)
            .Sum(f => f.QuantityLots);
        return PortfolioMarginEstimate.Estimate(_config.Capital + NetPnlNow(), Spot,
            _config.LotSize, shorts, futures, scenarios, _config.MarginExposureRate);
    }

    private List<PayoffPoint> BuildCurve(Func<double, double> pnlAt)
    {
        var step = 2 * PayoffRange / (PayoffGridPoints - 1);
        var curve = new List<PayoffPoint>(PayoffGridPoints);
        for (var i = 0; i < PayoffGridPoints; i++)
        {
            var s = Math.Round(Spot - PayoffRange + i * step);
            curve.Add(new PayoffPoint(s, pnlAt(s)));
        }
        return curve;
    }

    private RunSnapshot BuildSnapshot(List<CoachSuggestion> suggestions)
    {
        var snapshot = new RunSnapshot
        {
            Status = Status,
            TickIndex = TickIndex,
            SimMinute = SimMinute,
            MinutesToExpiry = Math.Round(MinutesToExpiry),
            Spot = Math.Round(Spot, 2),
            IvMultiplier = Math.Round(IvMultiplier, 3),
            FuturesPrice = Math.Round(CurrentFuturesPrice(), 2),
            AtmStrike = AtmStrike,
            Legs = _legs.ToList(),
            Futures = _futures.ToList(),
            Totals = BuildTotals(),
            Breakevens = ComputeBreakevens(),
            PayoffAtExpiry = ExpiryCurve(),
            PayoffToday = TodayCurve(),
            SpotHistory = _spotHistory.ToList(),
            Spikes = _spikes.TakeLast(50).ToList(),
            Suggestions = suggestions,
            Decisions = _decisions.TakeLast(DecisionsCap).ToList(),
        };
        LastSnapshot = snapshot;
        return snapshot;
    }

    public string DescribeStrategy() =>
        $"Sold {AtmStrike:0} CE/PE straddle + {AtmStrike + _config.WingOffset:0} CE / {AtmStrike - _config.WingOffset:0} PE strangle, 1 lot each";
}
