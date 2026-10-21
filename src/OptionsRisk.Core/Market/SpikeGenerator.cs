namespace OptionsRisk.Core.Market;

public readonly record struct Spike(double MagnitudeFraction, double IvMultiplierJump);

/// <summary>
/// Seeded event-day spike generator. Each call decides whether a spike fires
/// this tick; magnitude is uniform in [min, max] with a random direction.
/// </summary>
public sealed class SpikeGenerator(double probabilityPerTick, double minMagnitude, double maxMagnitude, double ivJumpPerPercent, int? seed = null)
{
    private readonly Random _rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public Spike? NextSpike()
    {
        if (_rng.NextDouble() >= probabilityPerTick)
            return null;

        var magnitude = minMagnitude + _rng.NextDouble() * (maxMagnitude - minMagnitude);
        var direction = _rng.NextDouble() < 0.5 ? -1.0 : 1.0;
        var signed = magnitude * direction;

        return new Spike(
            MagnitudeFraction: signed,
            IvMultiplierJump: magnitude * 100.0 * ivJumpPerPercent);
    }
}
