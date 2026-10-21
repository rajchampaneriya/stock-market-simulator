using OptionsRisk.Core.Pricing;

namespace OptionsRisk.Core.Strategy;

/// <summary>Aggregations over option legs and futures positions.</summary>
public static class PortfolioMath
{
    public static Greeks NetGreeks(
        IReadOnlyList<OptionLeg> legs,
        IReadOnlyList<FuturesPosition> futures,
        int lotSize,
        double riskFreeRate,
        double yearsToExpiry)
    {
        double delta = 0, gamma = 0, theta = 0, vega = 0;

        foreach (var leg in legs)
        {
            if (leg.Status != LegStatus.Open) continue;
            var units = leg.Units(lotSize);
            delta += units * leg.Quote.Greeks.Delta;
            gamma += units * leg.Quote.Greeks.Gamma;
            theta += units * leg.Quote.Greeks.ThetaPerDay;
            vega += units * leg.Quote.Greeks.VegaPerPoint;
        }

        var futDelta = FuturesMath.Delta(riskFreeRate, yearsToExpiry);
        foreach (var f in futures)
        {
            if (f.Status != LegStatus.Open) continue;
            delta += f.Units(lotSize) * futDelta;
        }

        return new Greeks(delta, gamma, theta, vega);
    }

    /// <summary>Net premium received (positive credit for shorts, debit for longs), ₹.</summary>
    public static double PremiumCollected(IReadOnlyList<OptionLeg> legs, int lotSize)
    {
        double total = 0;
        foreach (var leg in legs)
            total -= leg.Units(lotSize) * leg.EntryPrice;
        return total;
    }

    public static double RealizedPnl(IReadOnlyList<OptionLeg> legs, IReadOnlyList<FuturesPosition> futures, int lotSize)
    {
        double total = 0;
        foreach (var leg in legs) total += leg.RealizedPoints * Math.Abs(leg.QuantityLots) * lotSize;
        foreach (var f in futures) total += f.RealizedPoints * Math.Abs(f.QuantityLots) * lotSize;
        return total;
    }

    public static double UnrealizedPnl(IReadOnlyList<OptionLeg> legs, IReadOnlyList<FuturesPosition> futures, int lotSize)
    {
        double total = 0;
        foreach (var leg in legs) total += leg.UnrealizedPoints * Math.Abs(leg.QuantityLots) * lotSize;
        foreach (var f in futures) total += f.UnrealizedPoints * Math.Abs(f.QuantityLots) * lotSize;
        return total;
    }

    /// <summary>
    /// Total strategy P/L (₹) if spot finished at <paramref name="spot"/> at expiry.
    /// Open options settle at intrinsic, open futures converge to spot,
    /// exited positions contribute their realized P/L.
    /// </summary>
    public static double ExpiryPnl(
        IReadOnlyList<OptionLeg> legs,
        IReadOnlyList<FuturesPosition> futures,
        int lotSize,
        double spot)
    {
        double total = 0;

        foreach (var leg in legs)
        {
            var units = Math.Abs(leg.QuantityLots) * lotSize;
            total += leg.Status == LegStatus.Open
                ? leg.Direction * (leg.Intrinsic(spot) - leg.EntryPrice) * units
                : leg.RealizedPoints * units;
        }

        foreach (var f in futures)
        {
            var units = Math.Abs(f.QuantityLots) * lotSize;
            total += f.Status == LegStatus.Open
                ? f.Direction * (spot - f.EntryPrice) * units
                : f.RealizedPoints * units;
        }

        return total;
    }
}
