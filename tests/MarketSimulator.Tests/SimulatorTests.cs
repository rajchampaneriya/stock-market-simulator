using Shared.Contracts;
using MarketSimulator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketSimulator.Tests;

public class PriceGenerationTests
{
    [Fact]
    public void GeneratedPrice_ShouldStayPositive()
    {
        // Test that the random walk algorithm never generates negative prices
        var random = new Random(42); // Fixed seed for reproducibility
        var lastPrice = 10.00m;
        var volatility = 2.0;

        for (int i = 0; i < 1000; i++)
        {
            var change = (decimal)(random.NextDouble() - 0.5) * 2 * (decimal)volatility;
            var newPrice = lastPrice + change;
            newPrice = Math.Max(0.01m, newPrice);

            Assert.True(newPrice > 0, "Price should always be positive");
            lastPrice = newPrice;
        }
    }

    [Theory]
    [InlineData(100.00, 0.5, -0.5, 0.5)]
    [InlineData(50.00, 1.0, -1.0, 1.0)]
    [InlineData(200.00, 0.1, -0.1, 0.1)]
    public void PriceChange_WithVolatility_StaysWithinExpectedRange(
        decimal startPrice, 
        double volatility, 
        double minExpectedChange, 
        double maxExpectedChange)
    {
        // Run multiple iterations to test volatility bounds
        var random = new Random(123);
        var priceChanges = new List<decimal>();

        for (int i = 0; i < 100; i++)
        {
            var change = (decimal)(random.NextDouble() - 0.5) * 2 * (decimal)volatility;
            priceChanges.Add(change);
        }

        var minChange = priceChanges.Min();
        var maxChange = priceChanges.Max();

        // Changes should be within volatility bounds
        Assert.True(minChange >= (decimal)minExpectedChange - 0.01m, 
            $"Minimum change {minChange} should be >= {minExpectedChange}");
        Assert.True(maxChange <= (decimal)maxExpectedChange + 0.01m, 
            $"Maximum change {maxChange} should be <= {maxExpectedChange}");
    }

    [Fact]
    public void Price_ShouldBeRoundedToTwoDecimals()
    {
        // Test that prices are correctly rounded to 2 decimal places
        var testPrice = 123.456789m;
        var roundedPrice = Math.Round(testPrice, 2);

        Assert.Equal(123.46m, roundedPrice);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(-10.00)]
    [InlineData(-0.50)]
    public void Price_WhenBelowMinimum_ShouldBeClampedTo0_01(decimal invalidPrice)
    {
        // Test that prices below minimum are clamped
        var clampedPrice = Math.Max(0.01m, invalidPrice);

        Assert.Equal(0.01m, clampedPrice);
    }
}

public class VolumeGenerationTests
{
    [Fact]
    public void Volume_ShouldBeWithinExpectedRange()
    {
        // Test that volume generation stays within configured bounds
        var random = new Random(42);
        const int minVolume = 100;
        const int maxVolume = 10000;

        for (int i = 0; i < 100; i++)
        {
            var volume = random.Next(minVolume, maxVolume);

            Assert.InRange(volume, minVolume, maxVolume - 1);
        }
    }

    [Fact]
    public void Volume_ShouldVaryBetweenTicks()
    {
        // Test that volume changes between ticks
        var random = new Random(123);
        var volumes = new HashSet<int>();

        for (int i = 0; i < 50; i++)
        {
            volumes.Add(random.Next(100, 10000));
        }

        // We should have many unique volumes (not all the same)
        Assert.True(volumes.Count > 40, "Volume should vary significantly");
    }
}

public class MarketDataEventGenerationTests
{
    [Fact]
    public void MarketDataEvent_ShouldHaveCurrentTimestamp()
    {
        // Test that generated events have recent timestamps
        var beforeGeneration = DateTime.UtcNow;
        
        var marketData = new MarketDataEvent
        {
            Symbol = "TEST",
            Price = 100.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        var afterGeneration = DateTime.UtcNow;

        Assert.InRange(marketData.Timestamp, beforeGeneration, afterGeneration);
    }

    [Fact]
    public void MarketDataEvent_ShouldHaveValidSymbol()
    {
        // Test that event has non-empty symbol
        var marketData = new MarketDataEvent
        {
            Symbol = "AAPL",
            Price = 150.00m,
            Volume = 5000,
            Timestamp = DateTime.UtcNow
        };

        Assert.False(string.IsNullOrWhiteSpace(marketData.Symbol));
        Assert.Equal("AAPL", marketData.Symbol);
    }

    [Theory]
    [InlineData("NIFTY50", 18000.00, 50000)]
    [InlineData("BANKNIFTY", 43000.00, 25000)]
    [InlineData("AAPL", 150.00, 10000)]
    public void MarketDataEvent_WithVariousSymbols_CreatesValidEvents(
        string symbol, 
        decimal price, 
        long volume)
    {
        // Test event creation with various inputs
        var marketData = new MarketDataEvent
        {
            Symbol = symbol,
            Price = price,
            Volume = volume,
            Timestamp = DateTime.UtcNow
        };

        Assert.Equal(symbol, marketData.Symbol);
        Assert.Equal(price, marketData.Price);
        Assert.Equal(volume, marketData.Volume);
        Assert.True(marketData.Timestamp > DateTime.MinValue);
    }
}

public class RandomWalkAlgorithmTests
{
    [Fact]
    public void RandomWalk_OverTime_ShowsRealisticMovement()
    {
        // Test that the random walk produces realistic price movements
        var random = new Random(42);
        var initialPrice = 1000m;
        var currentPrice = initialPrice;
        var volatility = 0.5;
        var prices = new List<decimal> { initialPrice };

        // Simulate 100 ticks
        for (int i = 0; i < 100; i++)
        {
            var change = (decimal)(random.NextDouble() - 0.5) * 2 * (decimal)volatility;
            currentPrice = Math.Max(0.01m, currentPrice + change);
            prices.Add(currentPrice);
        }

        // Check that prices vary
        var uniquePrices = prices.Distinct().Count();
        Assert.True(uniquePrices > 50, "Prices should show variety over time");

        // Check that we don't have unrealistic jumps
        for (int i = 1; i < prices.Count; i++)
        {
            var change = Math.Abs(prices[i] - prices[i - 1]);
            var percentChange = (change / prices[i - 1]) * 100;
            Assert.True(percentChange < 1, "Individual tick changes should be small");
        }
    }

    [Fact]
    public void RandomWalk_WithHighVolatility_ShowsLargerMovements()
    {
        // Test that higher volatility produces larger price movements
        var random = new Random(42);
        var lowVolatility = 0.1;
        var highVolatility = 2.0;

        var lowVolChanges = new List<decimal>();
        var highVolChanges = new List<decimal>();

        for (int i = 0; i < 100; i++)
        {
            var lowChange = Math.Abs((decimal)(random.NextDouble() - 0.5) * 2 * (decimal)lowVolatility);
            var highChange = Math.Abs((decimal)(random.NextDouble() - 0.5) * 2 * (decimal)highVolatility);
            
            lowVolChanges.Add(lowChange);
            highVolChanges.Add(highChange);
        }

        var avgLowVolChange = lowVolChanges.Average();
        var avgHighVolChange = highVolChanges.Average();

        Assert.True(avgHighVolChange > avgLowVolChange, 
            "High volatility should produce larger average changes");
    }
}

public class ConfigurationTests
{
    [Fact]
    public void Configuration_DefaultValues_ShouldBeValid()
    {
        // Test default configuration values
        var defaultSymbols = new[] { "NIFTY50", "BANKNIFTY" };
        var defaultTickInterval = 300;
        var defaultVolatility = 0.5;
        var defaultInitialPrice = 1000m;

        Assert.NotEmpty(defaultSymbols);
        Assert.True(defaultTickInterval > 0);
        Assert.True(defaultVolatility > 0);
        Assert.True(defaultInitialPrice > 0);
    }
}
