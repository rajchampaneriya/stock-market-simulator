using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// Net-delta rule: a rally leaves a short-options book net short delta (buy futures),
/// a fall leaves it net long delta (sell futures). Suggests the lot count that
/// brings |net delta| back under the configured threshold.
/// </summary>
public sealed class DeltaHedgeRule : ICoachRule
{
    public string Name => "Delta Hedge";

    public IEnumerable<CoachSuggestion> Evaluate(CoachContext ctx)
    {
        var netDelta = ctx.Totals.NetGreeks.Delta;
        var threshold = ctx.Config.DeltaHedgeThresholdLots * ctx.Config.LotSize;
        if (Math.Abs(netDelta) <= threshold) yield break;

        var lots = (int)Math.Max(1, RuleMath.FuturesLotsForDelta(ctx, netDelta));
        var kind = netDelta < 0 ? CoachActionKind.BuyFutures : CoachActionKind.SellFutures;
        var direction = netDelta < 0 ? "rally" : "fall";
        var hedgeWord = netDelta < 0 ? "buy" : "sell";

        var (projection, _) = ProjectionEstimator.Estimate(ctx, new CoachAction(kind, lots), a => RuleMath.GreeksAfter(ctx, a));

        yield return new CoachSuggestion
        {
            Id = $"delta-hedge-{(netDelta < 0 ? "buy" : "sell")}",
            Rule = Name,
            Title = $"{hedgeWord.ToUpper()} {lots} NIFTY future lot(s)",
            Explanation =
                $"The {direction} has left the book net delta {netDelta:+0;-0} " +
                $"({netDelta / ctx.Config.LotSize:+0.0;-0.0} lot-deltas), beyond the ±{ctx.Config.DeltaHedgeThresholdLots} threshold. " +
                $"A disciplined seller hedges direction instead of hoping: {hedgeWord} {lots} future lot(s) to flatten delta while keeping theta income.",
            Action = new CoachAction(kind, lots),
            Projection = projection,
            FirstRaisedTick = ctx.TickIndex,
        };
    }
}
