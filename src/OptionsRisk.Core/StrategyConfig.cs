namespace OptionsRisk.Core;

/// <summary>
/// All tunable values for one educational options-selling run.
/// NSE revises lot sizes, strike steps and expiry rules periodically,
/// so nothing here is a constant.
/// </summary>
public record StrategyConfig
{
    /// <summary>Starting NIFTY spot for the run (rounded-down engine base).</summary>
    public double StartingSpot { get; init; } = 25_000;

    /// <summary>Contract lot size (NSE revises periodically).</summary>
    public int LotSize { get; init; } = 75;

    /// <summary>Strike interval in index points (50 for NIFTY).</summary>
    public double StrikeStep { get; init; } = 50;

    /// <summary>Distance of the short strangle wings from ATM.</summary>
    public double WingOffset { get; init; } = 500;

    /// <summary>Days to expiry at run start.</summary>
    public double DaysToExpiry { get; init; } = 3;

    /// <summary>Trading minutes per session (NSE: 9:15–15:30).</summary>
    public int TradingMinutesPerDay { get; init; } = 375;

    /// <summary>Simulated minutes advanced per engine tick.</summary>
    public double SimMinutesPerTick { get; init; } = 1;

    /// <summary>Risk-free rate used by Black-Scholes and cost of carry.</summary>
    public double RiskFreeRate { get; init; } = 0.065;

    /// <summary>Base ATM implied volatility before skew and spike adjustments.</summary>
    public double BaseIv { get; init; } = 0.13;

    /// <summary>Skew slope — IV added per 1% moneyness away from spot (put wing steeper).</summary>
    public double SkewSlope { get; init; } = 0.35;

    /// <summary>Annualised volatility for the underlying random walk.</summary>
    public double UnderlyingAnnualVol { get; init; } = 0.13;

    /// <summary>Mean reversion strength of the underlying walk.</summary>
    public double MeanReversionStrength { get; init; } = 0.0015;

    /// <summary>Probability of a spike on any single tick.</summary>
    public double SpikeProbabilityPerTick { get; init; } = 0.004;

    /// <summary>Minimum spike magnitude as fraction of spot (1%).</summary>
    public double SpikeMinMagnitude { get; init; } = 0.01;

    /// <summary>Maximum spike magnitude as fraction of spot (3%).</summary>
    public double SpikeMaxMagnitude { get; init; } = 0.03;

    /// <summary>IV multiplier added per 1% spike move.</summary>
    public double SpikeIvJumpPerPercent { get; init; } = 0.30;

    /// <summary>Fraction of spike-driven IV expansion retained after each tick.</summary>
    public double IvDecayPerTick { get; init; } = 0.97;

    /// <summary>Ceiling on the IV multiplier so quotes stay sane in a spike storm.</summary>
    public double IvMultiplierCap { get; init; } = 3.0;

    /// <summary>Coach: hedge when |net delta| exceeds this many lot-deltas.</summary>
    public double DeltaHedgeThresholdLots { get; init; } = 0.75;

    /// <summary>Coach: exit a leg when its premium reaches entry × this multiple.</summary>
    public double LegStopMultiple { get; init; } = 2.0;

    /// <summary>Coach: warn on drawdown beyond this fraction of capital.</summary>
    public double DrawdownFraction { get; init; } = 0.10;

    /// <summary>Coach: warn when the IV multiplier reaches this level after a spike.</summary>
    public double IvSpikeWarnMultiplier { get; init; } = 1.4;

    /// <summary>Ticks an ignored suggestion stays suppressed before it may re-fire.</summary>
    public int SuggestionCooldownTicks { get; init; } = 120;

    /// <summary>Virtual capital used for drawdown and sizing suggestions.</summary>
    public double Capital { get; init; } = 500_000;

    /// <summary>Auto-pause the run when a spike lands so the user must decide.</summary>
    public bool AutoPauseOnSpike { get; init; } = true;

    /// <summary>Seed for reproducible runs; null = random.</summary>
    public int? Seed { get; init; }

    /// <summary>Spot shock used to project "act vs hold" P/L (fraction of spot).</summary>
    public double ProjectionShockFraction { get; init; } = 0.01;

    /// <summary>Sim minutes into the future used to project "act vs hold" P/L.</summary>
    public double ProjectionHorizonMinutes { get; init; } = 30;
}
