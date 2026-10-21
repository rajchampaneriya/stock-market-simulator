namespace OptionsRisk.Core.Pricing;

public enum OptionType { Call, Put }

public readonly record struct OptionQuote(double Price, double Iv, Greeks Greeks);

public readonly record struct Greeks(double Delta, double Gamma, double ThetaPerDay, double VegaPerPoint);

/// <summary>
/// Pricing port — Black-Scholes today, swappable via configuration
/// (e.g. binomial or a vol-surface model) without touching strategy code.
/// </summary>
public interface IOptionPricer
{
    /// <summary>Price one option. Volatility supplied per leg by the caller
    /// so the skew/spike surface stays outside the pricing model.</summary>
    OptionQuote Price(OptionType type, double spot, double strike, double yearsToExpiry, double volatility);
}
