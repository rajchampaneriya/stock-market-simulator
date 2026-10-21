using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>An action the coach proposes. Payload identifies what to do when applied.</summary>
public enum CoachActionKind { BuyFutures, SellFutures, ExitLeg }

public sealed record CoachAction(CoachActionKind Kind, int Lots = 0, string? LegId = null);

/// <summary>Projected P/L (₹) over a short adverse horizon, with and without acting.</summary>
public sealed record Projection(double HoldPnl, double ActPnl, double ShockMove, double IvChangePoints);

public sealed record CoachSuggestion
{
    /// <summary>Stable id (rule + target) so repeated evaluations update in place.</summary>
    public required string Id { get; init; }
    public required string Rule { get; init; }
    public required string Title { get; init; }
    /// <summary>Plain-language "what a disciplined trader would consider and why".</summary>
    public required string Explanation { get; init; }
    public required CoachAction Action { get; init; }
    public required Projection Projection { get; init; }
    public required int FirstRaisedTick { get; init; }
}

public sealed record DecisionRecord(
    int TickIndex,
    int SimMinute,
    string Rule,
    string Description,
    string Outcome,          // "applied" | "ignored" | "opened" | "spike" | "expired"
    double? HoldPnl = null,
    double? ActPnl = null,
    double NetPnlAtDecision = 0,
    /// <summary>Filled on later ticks: net P/L change since the decision.</summary>
    double? OutcomePnl = null);

/// <summary>Everything a rule may look at on a tick. Read-only view of the run.</summary>
public sealed record CoachContext(
    StrategyConfig Config,
    double Spot,
    double IvMultiplier,
    double FuturesPrice,
    double MinutesToExpiry,
    double AtmStrike,
    IReadOnlyList<OptionLeg> Legs,
    IReadOnlyList<FuturesPosition> Futures,
    PortfolioTotals Totals,
    IReadOnlyList<double> Breakevens,
    int TickIndex);

/// <summary>
/// One pluggable coach rule. Implementations must be stateless —
/// dedup/cooldown state is managed by the engine, not the rule.
/// </summary>
public interface ICoachRule
{
    string Name { get; }

    /// <summary>Return suggestion(s) triggered by the current state, or empty.</summary>
    IEnumerable<CoachSuggestion> Evaluate(CoachContext context);
}
