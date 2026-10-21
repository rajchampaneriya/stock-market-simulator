namespace OptionsRisk.Core.Pricing;

/// <summary>
/// Closed-form Black-Scholes pricing and Greeks.
/// Theta is returned per calendar day, Vega per volatility point (1% = 0.01).
/// </summary>
public sealed class BlackScholesPricer(double riskFreeRate = 0.065) : IOptionPricer
{
    private const double MinYears = 1.0 / (365.0 * 24.0 * 60.0); // clamp to one minute

    public OptionQuote Price(OptionType type, double spot, double strike, double yearsToExpiry, double volatility)
    {
        var t = Math.Max(yearsToExpiry, MinYears);
        var sigma = Math.Max(volatility, 0.005);
        var sqrtT = Math.Sqrt(t);

        var d1 = (Math.Log(spot / strike) + (riskFreeRate + 0.5 * sigma * sigma) * t) / (sigma * sqrtT);
        var d2 = d1 - sigma * sqrtT;

        var disc = Math.Exp(-riskFreeRate * t);
        var pdf = NormalPdf(d1);

        double price, delta, theta;
        var vegaRaw = spot * pdf * sqrtT; // per 1.0 vol
        var gamma = disc * pdf / (spot * sigma * sqrtT);

        if (type == OptionType.Call)
        {
            price = spot * NormalCdf(d1) - strike * disc * NormalCdf(d2);
            delta = NormalCdf(d1);
            theta = -(spot * pdf * sigma) / (2.0 * sqrtT)
                    - riskFreeRate * strike * disc * NormalCdf(d2);
        }
        else
        {
            price = strike * disc * NormalCdf(-d2) - spot * NormalCdf(-d1);
            delta = NormalCdf(d1) - 1.0;
            theta = -(spot * pdf * sigma) / (2.0 * sqrtT)
                    + riskFreeRate * strike * disc * NormalCdf(-d2);
        }

        return new OptionQuote(
            Price: Math.Max(price, 0.01),
            Iv: volatility,
            Greeks: new Greeks(
                Delta: delta,
                Gamma: gamma,
                ThetaPerDay: theta / 365.0,
                VegaPerPoint: vegaRaw / 100.0));
    }

    public static double NormalCdf(double x) => 0.5 * Erfc(-x / Math.Sqrt(2.0));

    public static double NormalPdf(double x) => Math.Exp(-0.5 * x * x) / Math.Sqrt(2.0 * Math.PI);

    /// <summary>Abramowitz–Stegun 7.1.26 rational approximation, ~1.5e-7 max error.</summary>
    private static double Erfc(double x)
    {
        var z = Math.Abs(x);
        var t = 1.0 / (1.0 + 0.5 * z);
        var poly = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 + t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 + t * (-0.82215223 + t * 0.17087277)))))))));
        return x >= 0 ? poly : 2.0 - poly;
    }
}
