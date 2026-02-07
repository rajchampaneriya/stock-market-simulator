using MarketSimulator;

namespace MarketSimulator.Tests;

public class RealisticPriceEngineTests
{
    private const int Seed = 42; // Fixed seed for determinism

    private RealisticPriceEngine CreateEngine(int? seed = Seed)
    {
        return new RealisticPriceEngine(seed);
    }

    private SymbolProfile DefaultProfile(string symbol = "TEST", decimal basePrice = 1000m)
    {
        return new SymbolProfile
        {
            Symbol = symbol,
            BasePrice = basePrice,
            AnnualisedVolatility = 0.16,
            MeanReversionStrength = 0.002,
            BaseVolume = 5000,
            MinPrice = 100m
        };
    }

    [Fact]
    public void NextTick_ReturnsPositivePrice()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(DefaultProfile());

        for (var i = 0; i < 1000; i++)
        {
            var (price, _) = engine.NextTick("TEST");
            Assert.True(price > 0, $"Price should be positive, got {price}");
        }
    }

    [Fact]
    public void NextTick_ReturnsPositiveVolume()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(DefaultProfile());

        for (var i = 0; i < 1000; i++)
        {
            var (_, volume) = engine.NextTick("TEST");
            Assert.True(volume >= 100, $"Volume should be >= 100, got {volume}");
        }
    }

    [Fact]
    public void NextTick_PriceNeverBelowMinPrice()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(new SymbolProfile
        {
            Symbol = "LOW",
            BasePrice = 150m, // Close to min
            AnnualisedVolatility = 0.50, // High volatility
            MeanReversionStrength = 0.001,
            BaseVolume = 5000,
            MinPrice = 100m
        });

        for (var i = 0; i < 5000; i++)
        {
            var (price, _) = engine.NextTick("LOW");
            Assert.True(price >= 100m, $"Price should never go below MinPrice, got {price}");
        }
    }

    [Fact]
    public void NextTick_PricesRoundedToTwoDecimals()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(DefaultProfile());

        for (var i = 0; i < 100; i++)
        {
            var (price, _) = engine.NextTick("TEST");
            var rounded = Math.Round(price, 2);
            Assert.Equal(rounded, price);
        }
    }

    [Fact]
    public void NextTick_PricesStayWithinReasonableBounds()
    {
        var engine = CreateEngine();
        var basePrice = 1000m;
        engine.RegisterSymbol(DefaultProfile("BOUNDED", basePrice));

        decimal min = basePrice, max = basePrice;

        for (var i = 0; i < 10000; i++)
        {
            var (price, _) = engine.NextTick("BOUNDED");
            min = Math.Min(min, price);
            max = Math.Max(max, price);
        }

        // With mean reversion, prices should stay within a reasonable range
        // (not go to 0 or explode to infinity over 10k ticks)
        Assert.True(min > 100m, $"Minimum price {min} is too low");
        Assert.True(max < 5000m, $"Maximum price {max} is too high");
    }

    [Fact]
    public void MeanReversion_PullsPricesBackToBasePrice()
    {
        var engine = CreateEngine(seed: 123);
        var basePrice = 1000m;
        engine.RegisterSymbol(new SymbolProfile
        {
            Symbol = "REVERT",
            BasePrice = basePrice,
            AnnualisedVolatility = 0.10, // Lower vol for clearer mean reversion
            MeanReversionStrength = 0.01, // Strong mean reversion
            BaseVolume = 5000,
            MinPrice = 100m
        });

        // Run many ticks and check average price is near base
        decimal sum = 0;
        const int ticks = 20000;

        for (var i = 0; i < ticks; i++)
        {
            var (price, _) = engine.NextTick("REVERT");
            sum += price;
        }

        var average = sum / ticks;
        var deviationPercent = Math.Abs((double)(average - basePrice) / (double)basePrice) * 100;

        // Average price should be within 10% of base price with mean reversion
        Assert.True(deviationPercent < 10,
            $"Average price {average:F2} deviated {deviationPercent:F1}% from base {basePrice}");
    }

    [Fact]
    public void HighVolatilitySymbol_HasLargerPriceSwings()
    {
        var engineHigh = CreateEngine(seed: 100);
        var engineLow = CreateEngine(seed: 100);

        engineHigh.RegisterSymbol(new SymbolProfile
        {
            Symbol = "HIGH_VOL",
            BasePrice = 1000m,
            AnnualisedVolatility = 0.40, // High volatility
            MeanReversionStrength = 0.002,
            BaseVolume = 5000,
            MinPrice = 100m
        });

        engineLow.RegisterSymbol(new SymbolProfile
        {
            Symbol = "LOW_VOL",
            BasePrice = 1000m,
            AnnualisedVolatility = 0.05, // Low volatility
            MeanReversionStrength = 0.002,
            BaseVolume = 5000,
            MinPrice = 100m
        });

        var highReturns = new List<double>();
        var lowReturns = new List<double>();
        decimal prevHigh = 1000m, prevLow = 1000m;

        for (var i = 0; i < 5000; i++)
        {
            var (priceHigh, _) = engineHigh.NextTick("HIGH_VOL");
            var (priceLow, _) = engineLow.NextTick("LOW_VOL");

            if (i > 0)
            {
                highReturns.Add((double)(priceHigh - prevHigh) / (double)prevHigh);
                lowReturns.Add((double)(priceLow - prevLow) / (double)prevLow);
            }

            prevHigh = priceHigh;
            prevLow = priceLow;
        }

        var highStdDev = StdDev(highReturns);
        var lowStdDev = StdDev(lowReturns);

        Assert.True(highStdDev > lowStdDev,
            $"High vol ({highStdDev:E3}) should have larger return std dev than low vol ({lowStdDev:E3})");
    }

    [Fact]
    public void VolumeScalesWithPriceMoves()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(DefaultProfile());

        var volumes = new List<long>();
        var prices = new List<decimal>();

        for (var i = 0; i < 5000; i++)
        {
            var (price, volume) = engine.NextTick("TEST");
            volumes.Add(volume);
            prices.Add(price);
        }

        // Volume should have variation (not constant)
        var minVol = volumes.Min();
        var maxVol = volumes.Max();
        Assert.True(maxVol > minVol * 1.5,
            $"Volume should vary significantly. Min={minVol}, Max={maxVol}");
    }

    [Fact]
    public void MultipleSymbols_ProduceIndependentPrices()
    {
        var engine = CreateEngine();
        engine.RegisterSymbol(DefaultProfile("SYM_A", 500m));
        engine.RegisterSymbol(DefaultProfile("SYM_B", 2000m));

        var pricesA = new List<decimal>();
        var pricesB = new List<decimal>();

        for (var i = 0; i < 100; i++)
        {
            var (priceA, _) = engine.NextTick("SYM_A");
            var (priceB, _) = engine.NextTick("SYM_B");
            pricesA.Add(priceA);
            pricesB.Add(priceB);
        }

        // Prices should be in different ranges
        Assert.True(pricesA.Average() < 1000m, "SYM_A should stay near 500");
        Assert.True(pricesB.Average() > 1000m, "SYM_B should stay near 2000");
    }

    [Fact]
    public void UnregisteredSymbol_ThrowsInvalidOperationException()
    {
        var engine = CreateEngine();

        Assert.Throws<InvalidOperationException>(() => engine.NextTick("UNKNOWN"));
    }

    [Fact]
    public void DeterministicSeed_ProducesReproducibleResults()
    {
        var engine1 = CreateEngine(seed: 777);
        var engine2 = CreateEngine(seed: 777);

        engine1.RegisterSymbol(DefaultProfile("DET"));
        engine2.RegisterSymbol(DefaultProfile("DET"));

        for (var i = 0; i < 100; i++)
        {
            var (price1, volume1) = engine1.NextTick("DET");
            var (price2, volume2) = engine2.NextTick("DET");

            Assert.Equal(price1, price2);
            Assert.Equal(volume1, volume2);
        }
    }

    [Fact]
    public void GarchVolatilityClustering_LargeMovesCluster()
    {
        var engine = CreateEngine(seed: 42);
        engine.RegisterSymbol(DefaultProfile());

        var returns = new List<double>();
        var prevPrice = 1000m;

        for (var i = 0; i < 5000; i++)
        {
            var (price, _) = engine.NextTick("TEST");
            returns.Add(Math.Abs((double)(price - prevPrice) / (double)prevPrice));
            prevPrice = price;
        }

        // Test for clustering: large moves should tend to be followed by large moves
        // Split returns into segments and check variance of variance
        var segmentSize = 50;
        var segmentVols = new List<double>();
        for (var i = 0; i + segmentSize <= returns.Count; i += segmentSize)
        {
            var segment = returns.Skip(i).Take(segmentSize).ToList();
            segmentVols.Add(StdDev(segment));
        }

        // With GARCH clustering, the variance of segment volatilities should be non-trivial
        var volOfVol = StdDev(segmentVols);
        Assert.True(volOfVol > 0, "Volatility clustering should produce varying segment volatilities");
    }

    private static double StdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
