namespace OptionsRisk.Core.Pricing;

/// <summary>
/// Simple deterministic volatility skew: OTM puts trade richer than OTM calls
/// (equity-index smile). IV = base × (1 + slope × moneyness) × spike multiplier,
/// where moneyness = strike/spot − 1 (negative for puts below spot).
/// </summary>
public sealed class VolatilitySurface(double baseIv, double skewSlope)
{
    private const double MinIv = 0.05;
    private const double MaxIv = 2.00;

    public double IvFor(OptionType type, double spot, double strike, double ivMultiplier)
    {
        var moneyness = strike / spot - 1.0;

        // Calls: smile rises slowly above spot; Puts: smile rises faster below spot.
        var tilt = type == OptionType.Put
            ? -moneyness                       // put wing: OTM (K < S) → positive add
            : moneyness * 0.5;                 // call wing: half slope, mildly positive OTM

        var iv = baseIv * (1.0 + skewSlope * tilt) * ivMultiplier;
        return Math.Clamp(iv, MinIv, MaxIv);
    }
}
