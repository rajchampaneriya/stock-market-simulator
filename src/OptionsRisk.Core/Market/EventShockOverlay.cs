namespace OptionsRisk.Core.Market;

/// <summary>
/// Applies event-day price jumps as persistent log-price displacements on top of
/// the existing seeded GBM/OU/GARCH price path. Without an overlay the next
/// unshocked base-engine tick would erase an entire 1-3% spike immediately.
/// This is a scenario model, not a calibrated market-volatility model.
/// </summary>
public sealed class EventShockOverlay
{
    private readonly double _retention;
    private double _logShock;

    /// <param name="retentionPerTick">Fraction of the log-price shock retained on each subsequent tick; 1 = permanent.</param>
    public EventShockOverlay(double retentionPerTick)
    {
        if (!double.IsFinite(retentionPerTick) || retentionPerTick is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(retentionPerTick));
        _retention = retentionPerTick;
    }

    /// <summary>Unshocked observed level immediately before any new spike this tick.</summary>
    public double BeforeSpike(double baseSpot)
    {
        CheckSpot(baseSpot);
        return Math.Round(baseSpot * Math.Exp(_logShock * _retention), 2);
    }

    /// <summary>
    /// Advances the overlay once per base-engine tick and optionally adds a new
    /// percentage shock. The next tick retains the displacement rather than
    /// snapping back to the base-engine price.
    /// </summary>
    public double Next(double baseSpot, double? spikeFraction = null)
    {
        CheckSpot(baseSpot);
        if (spikeFraction is { } shock && (!double.IsFinite(shock) || shock <= -1 || shock > 1))
            throw new ArgumentOutOfRangeException(nameof(spikeFraction));

        _logShock *= _retention;
        if (spikeFraction.HasValue)
            _logShock += Math.Log(1.0 + spikeFraction.Value);

        return Math.Round(baseSpot * Math.Exp(_logShock), 2);
    }

    private static void CheckSpot(double spot)
    {
        if (!double.IsFinite(spot) || spot <= 0)
            throw new ArgumentOutOfRangeException(nameof(spot));
    }
}
