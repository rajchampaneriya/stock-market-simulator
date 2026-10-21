using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Costs;
using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Coach;

/// <summary>
/// Greek-based "act vs hold" projection over a short adverse horizon.
/// Hold P/L uses the current portfolio greeks; act P/L re-derives net greeks
/// after hypothetically applying the suggestion. Educational approximation —
/// assumes a single adverse spot shock with no path dependence.
/// </summary>
public static class ProjectionEstimator
{
    public static (Projection Pnl, Greeks AfterGreeks) Estimate(
        CoachContext ctx, CoachAction action, Func<CoachAction, Greeks>? greeksAfterAction = null)
    {
        var g = ctx.Totals.NetGreeks;
        var shock = ctx.Config.ProjectionShockFraction * ctx.Spot;

        // Adverse direction: whichever spot move hurts the current book.
        var dS = Math.Abs(g.Delta) < 1e-9
            ? shock                      // delta-neutral short book: gamma decides, either side loses
            : -Math.Sign(g.Delta) * shock;

        // Adverse IV: short vega → IV up hurts.
        var dIvPoints = g.VegaPerPoint < 0 ? 1.0 : -1.0;

        var minutesPerYear = ctx.Config.TradingMinutesPerDay * 252.0;
        var years = ctx.Config.ProjectionHorizonMinutes / minutesPerYear;

        var after = greeksAfterAction?.Invoke(action) ?? g;
        var hold = g.Delta * dS + 0.5 * g.Gamma * dS * dS + g.ThetaPerDay * years * 365.0 + g.VegaPerPoint * dIvPoints;
        var act = after.Delta * dS + 0.5 * after.Gamma * dS * dS + after.ThetaPerDay * years * 365.0 + after.VegaPerPoint * dIvPoints
            - ActionCost(ctx, action);

        return (new Projection(
            HoldPnl: Math.Round(hold),
            ActPnl: Math.Round(act),
            ShockMove: Math.Round(dS),
            IvChangePoints: dIvPoints), after);
    }

    private static double ActionCost(CoachContext ctx, CoachAction action)
    {
        var cfg = ctx.Config;
        var fees = IndianFeeSchedule.ForBroker(cfg.BrokerProfile);
        if (action.Kind == CoachActionKind.ExitLeg)
        {
            var leg = ctx.Legs.FirstOrDefault(l => l.Id == action.LegId && l.Status == LegStatus.Open);
            if (leg is null) return 0;
            var ticks = ctx.IvMultiplier >= cfg.IvSpikeWarnMultiplier
                ? cfg.SpikeOptionSlippageTicks : cfg.NormalOptionSlippageTicks;
            var fill = SimulatedFill.Price(leg.LastPrice, TradeSide.Buy, cfg.OptionTickSize, ticks);
            return IndianDerivativesCosts.Calculate(DerivativeKind.Option, TradeSide.Buy,
                fill, leg.LastPrice, leg.QuantityLots * cfg.LotSize, fees).Total;
        }
        if (action.Lots <= 0) return 0;
        var side = action.Kind == CoachActionKind.BuyFutures ? TradeSide.Buy : TradeSide.Sell;
        var futFill = SimulatedFill.Price(ctx.FuturesPrice, side,
            cfg.FuturesTickSize, cfg.FuturesSlippageTicks);
        return IndianDerivativesCosts.Calculate(DerivativeKind.Future, side, futFill,
            ctx.FuturesPrice, action.Lots * cfg.LotSize, fees).Total;
    }
}
