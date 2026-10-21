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
        var lotDelta = ctx.Config.LotSize; // futures delta ≈ 1 per unit
        return Math.Ceiling(Math.Abs(netDelta) / lotDelta);
    }
}
