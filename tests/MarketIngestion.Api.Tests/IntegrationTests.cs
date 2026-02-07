using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using StackExchange.Redis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;

namespace MarketIngestion.Api.Tests;

/// <summary>
/// Integration tests for the MarketIngestion.Api /api/marketdata endpoint.
/// Uses WebApplicationFactory to test the actual API validation and endpoint behavior,
/// replacing the previous approach of copy-pasting validation logic into the test file.
/// </summary>
public class ApiIntegrationTests : IClassFixture<MarketIngestionApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(MarketIngestionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostMarketData_WithValidData_Returns200()
    {
        var validData = new MarketDataEvent
        {
            Symbol = "AAPL",
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithEmptySymbol_Returns400()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "",
            Price = 150.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithNegativePrice_Returns400()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "GOOGL",
            Price = -10.00m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithZeroPrice_Returns400()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "GOOGL",
            Price = 0m,
            Volume = 1000,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithNegativeVolume_Returns400()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "MSFT",
            Price = 330.00m,
            Volume = -500,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithZeroVolume_Returns200()
    {
        var validData = new MarketDataEvent
        {
            Symbol = "TSLA",
            Price = 200.00m,
            Volume = 0,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithFutureTimestamp_Returns400()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "NVDA",
            Price = 500.00m,
            Volume = 2000,
            Timestamp = DateTime.UtcNow.AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithRecentFutureTimestamp_WithinTolerance_Returns200()
    {
        var validData = new MarketDataEvent
        {
            Symbol = "AMD",
            Price = 100.00m,
            Volume = 5000,
            Timestamp = DateTime.UtcNow.AddMinutes(3)
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithPastTimestamp_Returns200()
    {
        var validData = new MarketDataEvent
        {
            Symbol = "INTC",
            Price = 50.00m,
            Volume = 3000,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_WithMultipleErrors_Returns400WithAllErrors()
    {
        var invalidData = new MarketDataEvent
        {
            Symbol = "",
            Price = -10.00m,
            Volume = -500,
            Timestamp = DateTime.UtcNow.AddDays(1)
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", invalidData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // Validation problem response should contain multiple error keys
        Assert.Contains("Symbol is required", body);
        Assert.Contains("Price must be positive", body);
        Assert.Contains("Volume cannot be negative", body);
        Assert.Contains("Timestamp cannot be in the future", body);
    }

    [Theory]
    [InlineData("A", 0.01, 0)]
    [InlineData("VERYLONGSYMBOLNAME", 999999.99, 999999999)]
    [InlineData("123", 1.23, 1)]
    public async Task PostMarketData_WithEdgeCaseValidValues_Returns200(
        string symbol,
        decimal price,
        long volume)
    {
        var validData = new MarketDataEvent
        {
            Symbol = symbol,
            Price = price,
            Volume = volume,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostMarketData_ReturnsMessageId()
    {
        var validData = new MarketDataEvent
        {
            Symbol = "NIFTY50",
            Price = 1000.00m,
            Volume = 5000,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/marketdata", validData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("messageId", content);
    }
}

/// <summary>
/// Custom WebApplicationFactory that replaces the real Redis with a mock.
/// </summary>
public class MarketIngestionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("environment", "Development");

        builder.ConfigureServices(services =>
        {
            // Remove the real Redis registration
            var redisDescriptors = services
                .Where(d => d.ServiceType == typeof(IConnectionMultiplexer))
                .ToList();
            foreach (var descriptor in redisDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add a mock Redis
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(db => db.StreamAddAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<RedisValue?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("1234567890-0"));

            var mockRedis = new Mock<IConnectionMultiplexer>();
            mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDb.Object);

            services.AddSingleton(mockRedis.Object);
        });
    }
}
