using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis connection
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection);

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// Market data endpoint
app.MapPost("/api/marketdata", async (
    [FromBody] MarketDataEvent marketData,
    IConnectionMultiplexer redis,
    ILogger<Program> logger) =>
{
    // Validation
    if (string.IsNullOrWhiteSpace(marketData.Symbol))
    {
        logger.LogWarning("Invalid market data: missing symbol");
        return Results.BadRequest("Symbol is required");
    }

    if (marketData.Price <= 0)
    {
        logger.LogWarning("Invalid market data: invalid price");
        return Results.BadRequest("Price must be positive");
    }

    try
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(marketData);
        
        // Publish to Redis Stream
        var messageId = await db.StreamAddAsync(
            "marketdata:stream",
            "data",
            json);

        logger.LogDebug("Published {Symbol} to stream with ID {MessageId}", 
            marketData.Symbol, messageId);

        return Results.Ok(new { messageId = messageId.ToString() });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to publish market data for {Symbol}", 
            marketData.Symbol);
        return Results.StatusCode(500);
    }
})
.WithName("PublishMarketData");

app.Run();
