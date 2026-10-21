using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// Drawdown rule: when peak-to-trough loss exceeds a fraction of capital,
/// suggest reducing size by exiting the worst-performing open leg.
/// </summary>
public sealed class DrawdownRule : ICoachRule
{
    public string Name => "Drawdown";

    public IEnumerable<CoachSuggestion> Evaluate(CoachContext ctx)
    {
        if (ctx.Totals.DrawdownFraction < ctx.Config.DrawdownFraction) yield break;

        var worst = ctx.Legs.Concat<Position>(ctx.Futures)
            .Where(p => p.Status == LegStatus.Open)
            .OrderBy(p => p.UnrealizedPoints * Math.Abs(p.QuantityLots) * ctx.Config.LotSize)
            .FirstOrDefault();
        if (worst == null) yield break;

        CoachAction action = worst is OptionLeg ol
            ? new CoachAction(CoachActionKind.ExitLeg, LegId: ol.Id)
            : new CoachAction(CoachActionKind.ExitLeg, LegId: ctx.Legs
                .Where(l => l.Status == LegStatus.Open)
                .OrderBy(l => l.UnrealizedPoints)
                .Select(l => l.Id)
                .FirstOrDefault() ?? string.Empty);

        if (string.IsNullOrEmpty(action.LegId)) yield break;

        var (projection, _) = ProjectionEstimator.Estimate(ctx, action, a => RuleMath.GreeksAfter(ctx, a));

        yield return new CoachSuggestion
        {
            Id = "drawdown",
            Rule = Name,
            Title = $"Drawdown {ctx.Totals.DrawdownFraction:P1} of capital — reduce size",
            Explanation =
                $"Peak-to-trough drawdown is {ctx.Totals.DrawdownFraction:P1} of the ₹{ctx.Config.Capital:0} capital " +
                $"(worst point ₹{ctx.Totals.MaxLossSoFar:0}). Risk first, reward second: exit the worst leg " +
                $"({worst.Display}) to shrink exposure. Size that hurts this much on one move is size you cannot manage.",
            Action = action,
            Projection = projection,
            FirstRaisedTick = ctx.TickIndex,
        };
    }
}
