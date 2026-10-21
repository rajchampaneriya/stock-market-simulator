using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// Leg stop-loss: when a short leg's premium reaches entry × multiple (default 2×),
/// the loss on that leg has doubled — suggest exiting before it runs further.
/// </summary>
public sealed class LegStopLossRule : ICoachRule
{
    public string Name => "Leg Stop-Loss";

    public IEnumerable<CoachSuggestion> Evaluate(CoachContext ctx)
    {
        foreach (var leg in ctx.Legs.Where(l => l.Status == LegStatus.Open))
        {
            var stop = leg.EntryPrice * ctx.Config.LegStopMultiple;
            if (leg.LastPrice < stop) continue;

            var action = new CoachAction(CoachActionKind.ExitLeg, LegId: leg.Id);
            var (projection, _) = ProjectionEstimator.Estimate(ctx, action, a => RuleMath.GreeksAfter(ctx, a));

            yield return new CoachSuggestion
            {
                Id = $"stop-loss-{leg.Id}",
                Rule = Name,
                Title = $"Exit {leg.Display}",
                Explanation =
                    $"{leg.Display} was sold at {leg.EntryPrice:0.0} and now trades at {leg.LastPrice:0.0} — " +
                    $"{leg.LastPrice / leg.EntryPrice:0.0}× entry, past the {ctx.Config.LegStopMultiple:0.0}× stop. " +
                    "Sellers who let stopped legs ride turn a defined loss into an open-ended one. Take the hit, keep the rest of the structure.",
                Action = action,
                Projection = projection,
                FirstRaisedTick = ctx.TickIndex,
            };
        }
    }
}
