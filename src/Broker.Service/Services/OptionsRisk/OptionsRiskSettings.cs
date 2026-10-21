using OptionsRisk.Core;

namespace Broker.Service.Services.OptionsRisk;

/// <summary>
/// Host-side options for the educational options risk simulator.
/// Domain knobs map onto <see cref="StrategyConfig"/>; engine knobs stay here.
/// </summary>
public sealed class OptionsRiskSettings
{
    /// <summary>Base real-time interval per simulated tick at 1× speed.</summary>
    public int TickIntervalMs { get; set; } = 500;

    /// <summary>Allowed playback speeds (1×, 5×, 20×). Empty until bound from config.</summary>
    public int[] Speeds { get; set; } = [];

    public int[] EffectiveSpeeds => Speeds is { Length: > 0 } ? Speeds : [1, 5, 20];

    // --- StrategyConfig mirror (bound from the same section) ---
    public double StartingSpot { get; set; } = 25_000;
    public int LotSize { get; set; } = 75;
    public double StrikeStep { get; set; } = 50;
    public double WingOffset { get; set; } = 500;
    public double DaysToExpiry { get; set; } = 3;
    public int TradingMinutesPerDay { get; set; } = 375;
    public double SimMinutesPerTick { get; set; } = 1;
    public double RiskFreeRate { get; set; } = 0.065;
    public double BaseIv { get; set; } = 0.13;
    public double SkewSlope { get; set; } = 0.35;
    public double UnderlyingAnnualVol { get; set; } = 0.13;
    public double MeanReversionStrength { get; set; } = 0.0015;
    public double SpikeProbabilityPerTick { get; set; } = 0.004;
    public double SpikeMinMagnitude { get; set; } = 0.01;
    public double SpikeMaxMagnitude { get; set; } = 0.03;
    public double SpikeIvJumpPerPercent { get; set; } = 0.30;
    public double IvDecayPerTick { get; set; } = 0.97;
    public double SpikeRetentionPerTick { get; set; } = 0.995;
    public double IvMultiplierCap { get; set; } = 3.0;
    public double DeltaHedgeThresholdLots { get; set; } = 0.75;
    public double LegStopMultiple { get; set; } = 2.0;
    public double DrawdownFraction { get; set; } = 0.10;
    public double IvSpikeWarnMultiplier { get; set; } = 1.4;
    public int SuggestionCooldownTicks { get; set; } = 120;
    public double Capital { get; set; } = 500_000;
    public bool AutoPauseOnSpike { get; set; } = true;
    public double OptionTickSize { get; set; } = 0.05;
    public double FuturesTickSize { get; set; } = 0.05;
    public double NormalOptionSlippageTicks { get; set; } = 1;
    public double SpikeOptionSlippageTicks { get; set; } = 8;
    public double FuturesSlippageTicks { get; set; } = 2;
    public double MarginStressSpotFraction { get; set; } = 0.03;
    public double MarginStressIvMultiplier { get; set; } = 1.5;
    public double MarginExposureRate { get; set; } = 0.02;
    public bool EnforceEstimatedMargin { get; set; } = true;
    public int? Seed { get; set; }
    public double ProjectionShockFraction { get; set; } = 0.01;
    public double ProjectionHorizonMinutes { get; set; } = 30;

    public StrategyConfig ToStrategyConfig(int? seedOverride = null) => new()
    {
        StartingSpot = StartingSpot,
        LotSize = LotSize,
        StrikeStep = StrikeStep,
        WingOffset = WingOffset,
        DaysToExpiry = DaysToExpiry,
        TradingMinutesPerDay = TradingMinutesPerDay,
        SimMinutesPerTick = SimMinutesPerTick,
        RiskFreeRate = RiskFreeRate,
        BaseIv = BaseIv,
        SkewSlope = SkewSlope,
        UnderlyingAnnualVol = UnderlyingAnnualVol,
        MeanReversionStrength = MeanReversionStrength,
        SpikeProbabilityPerTick = SpikeProbabilityPerTick,
        SpikeMinMagnitude = SpikeMinMagnitude,
        SpikeMaxMagnitude = SpikeMaxMagnitude,
        SpikeIvJumpPerPercent = SpikeIvJumpPerPercent,
        IvDecayPerTick = IvDecayPerTick,
        SpikeRetentionPerTick = SpikeRetentionPerTick,
        IvMultiplierCap = IvMultiplierCap,
        DeltaHedgeThresholdLots = DeltaHedgeThresholdLots,
        LegStopMultiple = LegStopMultiple,
        DrawdownFraction = DrawdownFraction,
        IvSpikeWarnMultiplier = IvSpikeWarnMultiplier,
        SuggestionCooldownTicks = SuggestionCooldownTicks,
        Capital = Capital,
        AutoPauseOnSpike = AutoPauseOnSpike,
        OptionTickSize = OptionTickSize,
        FuturesTickSize = FuturesTickSize,
        NormalOptionSlippageTicks = NormalOptionSlippageTicks,
        SpikeOptionSlippageTicks = SpikeOptionSlippageTicks,
        FuturesSlippageTicks = FuturesSlippageTicks,
        MarginStressSpotFraction = MarginStressSpotFraction,
        MarginStressIvMultiplier = MarginStressIvMultiplier,
        MarginExposureRate = MarginExposureRate,
        EnforceEstimatedMargin = EnforceEstimatedMargin,
        Seed = seedOverride ?? Seed,
        ProjectionShockFraction = ProjectionShockFraction,
        ProjectionHorizonMinutes = ProjectionHorizonMinutes,
    };
}
