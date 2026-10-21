using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>Helpers shared by coach rules: post-action greeks and formatting.</summary>
public static class RuleMath
{
    /// <summary>Net portfolio greeks after hypothetically applying an action.</summary>
    public static Greeks GreeksAfter(CoachContext ctx, CoachAction action)
    {
        var openLegs = ctx.Legs.Where(l => l.Status == LegStatus.Open).ToList();
        var openFuts = ctx.Futures.Where(f => f.Status == LegStatus.Open).ToList();

        switch (action.Kind)
        {
            case CoachActionKind.BuyFutures:
            case CoachActionKind.SellFutures:
                var sign = action.Kind == CoachActionKind.BuyFutures ? 1 : -1;
                openFuts.Add(new FuturesPosition
                {
                    Id = "hypothetical",
                    Direction = sign,
                    QuantityLots = action.Lots,
                    EntryPrice = ctx.FuturesPrice,
                });
                break;

            case CoachActionKind.ExitLeg:
                openLegs.RemoveAll(l => l.Id == action.LegId);
                break;
        }

        var years = ctx.MinutesToExpiry / (ctx.Config.TradingMinutesPerDay * 252.0);
        return PortfolioMath.NetGreeks(openLegs, openFuts, ctx.Config.LotSize, ctx.Config.RiskFreeRate, years);
    }

    public static double FuturesLotsForDelta(CoachContext ctx, double netDelta)
    {
        var years = ctx.MinutesToExpiry / (ctx.Config.TradingMinutesPerDay * 252.0);
        var lotDelta = ctx.Config.LotSize * FuturesMath.Delta(ctx.Config.RiskFreeRate, years);
        var idealLots = Math.Abs(netDelta) / lotDelta;
        var lower = (int)Math.Floor(idealLots);
        var upper = lower + 1;
        var threshold = ctx.Config.DeltaHedgeThresholdLots * ctx.Config.LotSize;
        var lowerResidual = Math.Abs(netDelta) - lower * lotDelta;
        var upperResidual = upper * lotDelta - Math.Abs(netDelta);
        // Prefer the smallest hedge that satisfies the configured band;
        // otherwise minimize the absolute residual, not blindly round up.
        if (lower > 0 && lowerResidual <= threshold) return lower;
        if (upperResidual <= threshold) return upper;
        return lowerResidual <= upperResidual ? lower : upper;
    }
}
