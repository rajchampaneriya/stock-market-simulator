namespace OptionsRisk.Core.Market;

/// <summary>
/// Converts the existing tick engine's raw log returns (calibrated for wall-clock
/// subsecond ticks) into realistic simulated trading-minute increments.
/// EWMA normalizes the raw GARCH-like volatility, retaining sign, serial
/// structure and relative shocks without inheriting its incompatible time scale.
/// Explicit event-day spikes are added separately after this normalization.
/// </summary>
public sealed class IntradayPriceNormalizer
{
    private readonly double _targetSigma;
    private readonly double _maxMove;
    private readonly double _baseSpot;
    private readonly double _meanReversion;
    private double _spot;
    private double? _lastRaw;
    private double _rawVariance = 0.0001;

    public IntradayPriceNormalizer(double startingSpot, double annualVolatility,
        double tradingMinutesPerDay, double simulatedMinutesPerTick,
        double meanReversion = 0.0015, double maxSigmaMultiple = 4)
    {
        if (startingSpot <= 0 || annualVolatility < 0 || tradingMinutesPerDay <= 0 ||
            simulatedMinutesPerTick <= 0 || maxSigmaMultiple <= 0)
            throw new ArgumentOutOfRangeException(nameof(startingSpot));
        _baseSpot = _spot = startingSpot;
        _meanReversion = meanReversion;
        _targetSigma = annualVolatility * Math.Sqrt(
            simulatedMinutesPerTick / (252.0 * tradingMinutesPerDay));
        _maxMove = maxSigmaMultiple * _targetSigma;
    }

    public double Next(double rawSpot)
    {
        if (!double.IsFinite(rawSpot) || rawSpot <= 0)
            throw new ArgumentOutOfRangeException(nameof(rawSpot));
        if (_lastRaw is null)
        {
            _lastRaw = rawSpot;
            return Math.Round(_spot, 2);
        }
        var rawReturn = Math.Log(rawSpot / _lastRaw.Value);
        _lastRaw = rawSpot;
        // Use the prior EWMA estimate to avoid look-ahead normalization.
        var rawSigma = Math.Sqrt(Math.Max(1e-10, _rawVariance));
        var normalized = _targetSigma * rawReturn / rawSigma;
        normalized = Math.Clamp(normalized, -_maxMove, _maxMove);
        normalized -= _meanReversion * Math.Log(_spot / _baseSpot);
        _rawVariance = 0.95 * _rawVariance + 0.05 * rawReturn * rawReturn;
        _spot *= Math.Exp(normalized);
        return Math.Round(_spot, 2);
    }
}
