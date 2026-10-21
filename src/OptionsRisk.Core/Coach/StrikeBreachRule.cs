using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// Breach rule: when spot crosses an outer (wing) strike or a portfolio breakeven,
/// the threatened side is the one losing — suggest closing (or rolling) it.
/// </summary>
public sealed class StrikeBreachRule : ICoachRule
{
    public string Name => "Strike / Breakeven Breach";

    public IEnumerable<CoachSuggestion> Evaluate(CoachContext ctx)
    {
        var threatened = ThreatenedLeg(ctx);
        if (threatened == null) yield break;

        var action = new CoachAction(CoachActionKind.ExitLeg, LegId: threatened.Id);
        var (projection, _) = ProjectionEstimator.Estimate(ctx, action, a => RuleMath.GreeksAfter(ctx, a));

        var wing = threatened.Strike;
        var level = Math.Abs(ctx.Spot - wing) < Math.Abs(ctx.Spot - ctx.AtmStrike)
            ? $"wing strike {wing:0}"
            : "breakeven";

        yield return new CoachSuggestion
        {
            Id = $"breach-{threatened.Id}",
            Rule = Name,
            Title = $"Close threatened side: {threatened.Display}",
            Explanation =
                $"Spot {ctx.Spot:0} has crossed the {level} — {threatened.Display} is now the losing side and its " +
                "loss grows point-for-point with further movement. A disciplined seller closes (or rolls) the threatened " +
                "side rather than holding a broken wing while still collecting thin premium on the other.",
            Action = action,
            Projection = projection,
            FirstRaisedTick = ctx.TickIndex,
        };
    }

    /// <summary>The open short leg whose strike is breached by spot on its losing side.</summary>
    internal static OptionLeg? ThreatenedLeg(CoachContext ctx)
    {
        OptionLeg? best = null;
        foreach (var leg in ctx.Legs.Where(l => l.Status == LegStatus.Open))
        {
            var breached = leg.Type == Pricing.OptionType.Call
                ? ctx.Spot > leg.Strike      // short call loses above its strike
                : ctx.Spot < leg.Strike;     // short put loses below its strike

            // Wing strikes only — the ATM straddle legs are always "near" spot by design.
            var isWing = Math.Abs(leg.Strike - ctx.AtmStrike) >= ctx.Config.WingOffset / 2;
            if (!breached || !isWing) continue;

            if (best == null || Math.Abs(ctx.Spot - leg.Strike) < Math.Abs(ctx.Spot - best.Strike))
                best = leg;
        }

        if (best != null) return best;

        // No wing breach: check breakevens — a crossed breakeven with an open losing side still warrants action.
        var anyBreakevenCrossed = ctx.Breakevens.Any(be => Math.Abs(ctx.Spot - be) < ctx.Config.StrikeStep);
        return anyBreakevenCrossed ? DeepestLosingleg(ctx) : null;
    }

    private static OptionLeg? DeepestLosingleg(CoachContext ctx) =>
        ctx.Legs.Where(l => l.Status == LegStatus.Open)
            .OrderByDescending(l => (l.EntryPrice - l.LastPrice) * Math.Abs(l.QuantityLots))
            .FirstOrDefault();
}
