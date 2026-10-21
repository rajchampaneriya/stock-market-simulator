namespace OptionsRisk.Core.Pricing;

/// <summary>NIFTY futures priced as spot plus cost of carry: F = S·e^{r·T}.</summary>
public static class FuturesMath
{
    public static double Price(double spot, double riskFreeRate, double yearsToExpiry) =>
        spot * Math.Exp(riskFreeRate * Math.Max(yearsToExpiry, 0));

    /// <summary>Futures delta with respect to spot is e^{r·T} ≈ 1.</summary>
    public static double Delta(double riskFreeRate, double yearsToExpiry) =>
        Math.Exp(riskFreeRate * Math.Max(yearsToExpiry, 0));
}
