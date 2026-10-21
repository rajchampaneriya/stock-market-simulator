using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Margin;
using OptionsRisk.Core.Costs;
using OptionsRisk.Core.Pricing;

namespace OptionsRisk.Core.Strategy;

public enum RunStatus { NotStarted, Running, Paused, Finished }

public sealed record MarketTick(
    double Spot,
    double IvMultiplier,
    int SimMinute,
    SpikeEvent? Spike = null);

public sealed record SpikeEvent(int TickIndex, int SimMinute, double FromSpot, double ToSpot, double IvMultiplierJump);

public sealed record PayoffPoint(double Spot, double Pnl);

public sealed record PortfolioTotals
{
    public required double Realized { get; init; }
    public required double Unrealized { get; init; }
    // Realized/unrealized use executed fills (already incorporating slippage).
    public double Gross => Realized + Unrealized + Costs.Slippage;
    public double Net => Realized + Unrealized - (Costs.Total - Costs.Slippage);
    public ExecutionCost Costs { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public MarginEstimate Margin { get; init; } = new(0, 0, 0, 0, false, "Not evaluated");
    public required double PremiumCollected { get; init; }
    /// <summary>Worst net P/L reached during the run (≤ 0).</summary>
    public required double MaxLossSoFar { get; init; }
    /// <summary>Peak-to-trough drop as fraction of configured capital.</summary>
    public required double DrawdownFraction { get; init; }
    public required Greeks NetGreeks { get; init; }
}

public sealed record RunSnapshot
{
    public required RunStatus Status { get; init; }
    public required int TickIndex { get; init; }
    public required int SimMinute { get; init; }
    public required double MinutesToExpiry { get; init; }
    public required double Spot { get; init; }
    public required double IvMultiplier { get; init; }
    public required double FuturesPrice { get; init; }
    public required double AtmStrike { get; init; }
    public required IReadOnlyList<OptionLeg> Legs { get; init; }
    public required IReadOnlyList<FuturesPosition> Futures { get; init; }
    public required PortfolioTotals Totals { get; init; }
    public required IReadOnlyList<double> Breakevens { get; init; }
    public required IReadOnlyList<PayoffPoint> PayoffAtExpiry { get; init; }
    public required IReadOnlyList<PayoffPoint> PayoffToday { get; init; }
    public required IReadOnlyList<(int Tick, double Spot)> SpotHistory { get; init; }
    public required IReadOnlyList<SpikeEvent> Spikes { get; init; }
    public required IReadOnlyList<CoachSuggestion> Suggestions { get; init; }
    public required IReadOnlyList<DecisionRecord> Decisions { get; init; }
}
