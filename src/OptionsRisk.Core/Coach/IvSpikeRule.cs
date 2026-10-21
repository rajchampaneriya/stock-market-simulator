using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// IV-spike rule: after an event-day move, implied volatility expands and the
/// short book bleeds vega. Warn and offer to cut the highest-vega leg.
/// </summary>
public sealed class IvSpikeRule : ICoachRule
{
    public string Name => "IV Spike";

    public IEnumerable<CoachSuggestion> Evaluate(CoachContext ctx)
    {
        if (ctx.IvMultiplier < ctx.Config.IvSpikeWarnMultiplier) yield break;

        var worst = ctx.Legs
            .Where(l => l.Status == LegStatus.Open)
            .OrderByDescending(l => Math.Abs(l.Quote.Greeks.VegaPerPoint) * Math.Abs(l.QuantityLots) * ctx.Config.LotSize)
            .FirstOrDefault();
        if (worst == null) yield break;

        var action = new CoachAction(CoachActionKind.ExitLeg, LegId: worst.Id);
        var (projection, _) = ProjectionEstimator.Estimate(ctx, action, a => RuleMath.GreeksAfter(ctx, a));

        yield return new CoachSuggestion
        {
            Id = "iv-spike",
            Rule = Name,
            Title = $"IV spiked to {ctx.IvMultiplier:0.0}× — consider cutting {worst.Display}",
            Explanation =
                $"The spike expanded implied volatility to {ctx.IvMultiplier:0.0}× base. Every further vol point costs the book " +
                $"₹{Math.Abs(ctx.Totals.NetGreeks.VegaPerPoint):0} (net vega {ctx.Totals.NetGreeks.VegaPerPoint:0}). " +
                $"Cutting the highest-vega leg ({worst.Display}) reduces vega bleed; vol partly mean-reverts after events, " +
                "but holding the full short-vega book through the storm is what blows up sellers.",
            Action = action,
            Projection = projection,
            FirstRaisedTick = ctx.TickIndex,
        };
    }
}
