using Shared.Contracts;

namespace Shared.Contracts.Tests;

public class SecurityTests
{
    [Fact]
    public void Change_WhenPricesSet_ReturnsCorrectDifference()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "AAPL",
            LastPrice = 150.00m,
            PreviousPrice = 145.00m
        };

        // Act
        var change = security.Change;

        // Assert
        Assert.Equal(5.00m, change);
    }

    [Fact]
    public void Change_WhenPriceDecreases_ReturnsNegativeValue()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "GOOGL",
            LastPrice = 2800.00m,
            PreviousPrice = 2850.00m
        };

        // Act
        var change = security.Change;

        // Assert
        Assert.Equal(-50.00m, change);
    }

    [Fact]
    public void ChangePercent_WhenPriceIncreases_ReturnsCorrectPercentage()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "MSFT",
            LastPrice = 330.00m,
            PreviousPrice = 300.00m
        };

        // Act
        var changePercent = security.ChangePercent;

        // Assert
        Assert.Equal(10.00m, changePercent);
    }

    [Fact]
    public void ChangePercent_WhenPriceDecreases_ReturnsNegativePercentage()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "TSLA",
            LastPrice = 180.00m,
            PreviousPrice = 200.00m
        };

        // Act
        var changePercent = security.ChangePercent;

        // Assert
        Assert.Equal(-10.00m, changePercent);
    }

    [Fact]
    public void ChangePercent_WhenPreviousPriceIsZero_ReturnsZero()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "NVDA",
            LastPrice = 500.00m,
            PreviousPrice = 0m
        };

        // Act
        var changePercent = security.ChangePercent;

        // Assert
        Assert.Equal(0m, changePercent);
    }

    [Fact]
    public void ChangePercent_WithSmallPriceChange_ReturnsAccuratePercentage()
    {
        // Arrange
        var security = new Security
        {
            Symbol = "AMD",
            LastPrice = 100.50m,
            PreviousPrice = 100.00m
        };

        // Act
        var changePercent = security.ChangePercent;

        // Assert
        Assert.Equal(0.50m, changePercent);
    }

    [Theory]
    [InlineData(150.00, 145.00, 5.00)]
    [InlineData(100.00, 100.00, 0.00)]
    [InlineData(50.00, 100.00, -50.00)]
    public void ChangeCalculations_WithVariousScenarios_ReturnsExpectedValues(
        decimal lastPrice, 
        decimal previousPrice, 
        decimal expectedChange)
    {
        // Arrange
        var security = new Security
        {
            Symbol = "TEST",
            LastPrice = lastPrice,
            PreviousPrice = previousPrice
        };

        // Act
        var change = security.Change;
        var changePercent = security.ChangePercent;

        // Assert
        Assert.Equal(expectedChange, change);
        
        // Calculate expected change percent
        var expectedChangePercent = previousPrice > 0 
            ? (expectedChange / previousPrice) * 100 
            : 0;
        Assert.Equal(expectedChangePercent, changePercent);
    }
}

public class MarketDataEventTests
{
    [Fact]
    public void MarketDataEvent_CanBeCreatedWithInitValues()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var marketData = new MarketDataEvent
        {
            Symbol = "AAPL",
            Price = 150.00m,
            Volume = 1000000,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("AAPL", marketData.Symbol);
        Assert.Equal(150.00m, marketData.Price);
        Assert.Equal(1000000, marketData.Volume);
        Assert.Equal(timestamp, marketData.Timestamp);
    }

    [Fact]
    public void MarketDataEvent_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var event1 = new MarketDataEvent
        {
            Symbol = "GOOGL",
            Price = 2850.00m,
            Volume = 500000,
            Timestamp = timestamp
        };

        var event2 = new MarketDataEvent
        {
            Symbol = "GOOGL",
            Price = 2850.00m,
            Volume = 500000,
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.Equal(event1, event2);
    }

    [Fact]
    public void MarketDataEvent_RecordEquality_DetectsDifferences()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var event1 = new MarketDataEvent
        {
            Symbol = "MSFT",
            Price = 330.00m,
            Volume = 750000,
            Timestamp = timestamp
        };

        var event2 = new MarketDataEvent
        {
            Symbol = "MSFT",
            Price = 331.00m, // Different price
            Volume = 750000,
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void MarketDataEvent_WithOperator_CreatesNewInstance()
    {
        // Arrange
        var original = new MarketDataEvent
        {
            Symbol = "TSLA",
            Price = 200.00m,
            Volume = 2000000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var modified = original with { Price = 205.00m };

        // Assert
        Assert.Equal("TSLA", modified.Symbol);
        Assert.Equal(205.00m, modified.Price);
        Assert.Equal(2000000, modified.Volume);
        Assert.NotSame(original, modified);
    }
}
