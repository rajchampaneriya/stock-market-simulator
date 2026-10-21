using OptionsRisk.Core.Market;
using OptionsRisk.Core.Pricing;
using Xunit;

namespace OptionsRisk.Core.Tests;

public sealed class QuantCorrectnessTests
{
    [Fact]
    public void BlackScholesGamma_MatchesFiniteDifferenceOfDelta()
    {
        var p = new BlackScholesPricer(0.065);
        const double s = 25000, h = 0.5, t = 0.25, sigma = 0.20;
        var gamma = p.Price(OptionType.Call, s, s, t, sigma).Greeks.Gamma;
        var numerical = (p.Price(OptionType.Call, s+h, s, t, sigma).Greeks.Delta
                       - p.Price(OptionType.Call, s-h, s, t, sigma).Greeks.Delta)/(2*h);
        Assert.InRange(Math.Abs(gamma-numerical), 0, 1e-7);
    }

    [Fact]
    public void SpikeOverlay_RetainsShockInsteadOfReversingOnNextTick()
    {
        var overlay = new EventShockOverlay(0.995);
        Assert.Equal(25500, overlay.Next(25000, 0.02), 2);
        var following = overlay.Next(25000);
        Assert.InRange(following, 25490, 25500);
    }

    [Fact]
    public void SpikeOverlay_WithFixedInputsIsReproducible()
    {
        static double[] Path()
        {
            var overlay = new EventShockOverlay(0.99);
            return [overlay.Next(25000), overlay.Next(25001, -0.03), overlay.Next(25002),
                overlay.Next(25003, 0.02), overlay.Next(25004)];
        }
        Assert.Equal(Path(), Path());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    public void SpikeOverlay_RejectsInvalidRetention(double retention) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventShockOverlay(retention));
}
