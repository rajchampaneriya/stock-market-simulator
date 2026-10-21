namespace OptionsRisk.Core.Margin;

/// <summary>
/// Educational-only stress-based margin proxy, NOT NSE SPAN or a broker margin quote.
/// Does not represent portfolio offset rules or additional expiry-day requirements.
/// </summary>
public sealed record MarginScenario(double SpotShockFraction, double IvShockPoints, double ProjectedPnl);
public sealed record MarginEstimate(double StressLoss, double ExposureAddOn, double Required,
    double FreeCapital, bool MarginShortfall, string Method);

public static class PortfolioMarginEstimate
{
    public static MarginEstimate Estimate(double capital, double spot, int lotSize,
        int shortOptionLots, int futureLots, IReadOnlyList<MarginScenario> scenarios,
        double exposureRate = 0.02)
    {
        if (capital < 0 || spot <= 0 || lotSize <= 0 || shortOptionLots < 0 || futureLots < 0 ||
            exposureRate < 0 || scenarios.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(capital));
        var worstLoss = Math.Max(0, -scenarios.Min(s => s.ProjectedPnl));
        // Conservative additive approximation; not exchange-approved offset calculation.
        var exposure = exposureRate * spot * lotSize * (shortOptionLots + futureLots);
        var required = worstLoss + exposure;
        return new MarginEstimate(worstLoss, exposure, required, capital - required,
            required > capital, "Illustrative stress proxy — not SPAN; obtain basket quote");
    }
}
