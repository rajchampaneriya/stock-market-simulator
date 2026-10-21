using OptionsRisk.Core.Market;
using Xunit;

namespace OptionsRisk.Core.Tests;

public sealed class IntradayPriceNormalizerTests
{
    [Fact]
    public void RawEngineJumpIsScaledToOneMinuteVolatility()
    {
        var normalizer = new IntradayPriceNormalizer(25000, 0.13, 375, 1);
        Assert.Equal(25000, normalizer.Next(25000));
        var next = normalizer.Next(25500); // 2% raw engine jump is NOT an event spike
        Assert.InRange(next, 24950, 25050);
    }

    [Fact]
    public void SameRawPathIsDeterministic()
    {
        static double[] Path()
        {
            var normalizer = new IntradayPriceNormalizer(25000, 0.13, 375, 1);
            return new[] { 25000d, 25025, 25020, 25050, 24980, 25000 }
                .Select(normalizer.Next).ToArray();
        }
        Assert.Equal(Path(), Path());
    }

    [Fact]
    public void FlatRawPricesDoNotInventVolatility()
    {
        var normalizer = new IntradayPriceNormalizer(25000, 0.13, 375, 1);
        for (var i = 0; i < 20; i++) Assert.Equal(25000, normalizer.Next(25000));
    }
}
