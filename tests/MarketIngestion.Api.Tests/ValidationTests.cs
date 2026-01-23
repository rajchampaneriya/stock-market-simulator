using Shared.Contracts;

namespace MarketIngestion.Api.Tests;

public class ValidationTests
{
    private static List<string> ValidateMarketData(MarketDataEvent data)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(data.Symbol))
            errors.Add("Symbol is required");
        
        if (data.Price <= 0)
            errors.Add("Price must be positive");
        
        if (data.Volume < 0)
            errors.Add("Volume cannot be negative");
        
        if (data.Timestamp > DateTime.UtcNow.AddMinutes(5))
            errors.Add("Timestamp cannot be in the future");
        
        return errors;
    }

    [Fact]
    public void ValidateMarketData_WithValidData_ReturnsNoErrors()
    {
        // Arrange
        var validData = new MarketDataEvent
        {
            Symbol = "AAPL",
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(validData);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMarketData_WithEmptySymbol_ReturnsError()
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "",
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Symbol is required", errors);
    }

    [Fact]
    public void ValidateMarketData_WithNullSymbol_ReturnsError()
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = null!,
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Symbol is required", errors);
    }

    [Fact]
    public void ValidateMarketData_WithWhitespaceSymbol_ReturnsError()
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "   ",
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Symbol is required", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void ValidateMarketData_WithNonPositivePrice_ReturnsError(decimal price)
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "GOOGL",
            Price = price,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Price must be positive", errors);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1000)]
    [InlineData(-50)]
    public void ValidateMarketData_WithNegativeVolume_ReturnsError(long volume)
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "MSFT",
            Price = 330.00m,
            Volume = volume,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Volume cannot be negative", errors);
    }

    [Fact]
    public void ValidateMarketData_WithZeroVolume_ReturnsNoError()
    {
        // Arrange - Zero volume is valid
        var validData = new MarketDataEvent
        {
            Symbol = "TSLA",
            Price = 200.00m,
            Volume = 0,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(validData);

        // Assert
        Assert.DoesNotContain("Volume cannot be negative", errors);
    }

    [Fact]
    public void ValidateMarketData_WithFutureTimestamp_ReturnsError()
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "NVDA",
            Price = 500.00m,
            Volume = 2000,
            Timestamp = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Contains("Timestamp cannot be in the future", errors);
    }

    [Fact]
    public void ValidateMarketData_WithRecentFutureTimestamp_WithinTolerance_ReturnsNoError()
    {
        // Arrange - Within 5 minute tolerance
        var validData = new MarketDataEvent
        {
            Symbol = "AMD",
            Price = 100.00m,
            Volume = 5000,
            Timestamp = DateTime.UtcNow.AddMinutes(3)
        };

        // Act
        var errors = ValidateMarketData(validData);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMarketData_WithPastTimestamp_ReturnsNoError()
    {
        // Arrange - Past timestamps are valid
        var validData = new MarketDataEvent
        {
            Symbol = "INTC",
            Price = 50.00m,
            Volume = 3000,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var errors = ValidateMarketData(validData);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMarketData_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var invalidData = new MarketDataEvent
        {
            Symbol = "",
            Price = -10.00m,
            Volume = -500,
            Timestamp = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var errors = ValidateMarketData(invalidData);

        // Assert
        Assert.Equal(4, errors.Count);
        Assert.Contains("Symbol is required", errors);
        Assert.Contains("Price must be positive", errors);
        Assert.Contains("Volume cannot be negative", errors);
        Assert.Contains("Timestamp cannot be in the future", errors);
    }

    [Theory]
    [InlineData("A", 0.01, 0)]
    [InlineData("VERYLONGSYMBOLNAME", 999999.99, 999999999)]
    [InlineData("123", 1.23, 1)]
    public void ValidateMarketData_WithEdgeCaseValidValues_ReturnsNoErrors(
        string symbol, 
        decimal price, 
        long volume)
    {
        // Arrange
        var validData = new MarketDataEvent
        {
            Symbol = symbol,
            Price = price,
            Volume = volume,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var errors = ValidateMarketData(validData);

        // Assert
        Assert.Empty(errors);
    }
}
