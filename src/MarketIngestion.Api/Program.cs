using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis connection using Aspire service discovery
// The "cache" name matches the resource name in AppHost
builder.AddRedisClient("cache");

// Health checks
var redisConnection = builder.Configuration.GetConnectionString("cache") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection);

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    try
    {
        var validationErrors = ValidateMarketData(marketData);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Validation failed for {Symbol}: {Errors}", 
                marketData.Symbol, string.Join(", ", validationErrors));
            return Results.ValidationProblem(
                validationErrors.ToDictionary(e => e, e => new[] { e }));
        }

        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(marketData);
        
        var messageId = await db.StreamAddAsync(
            "marketdata:stream",
            "data",
            json,
            maxLength: 10000,
            useApproximateMaxLength: true);

        logger.LogDebug("Published {Symbol} to stream with ID {MessageId}", 
            marketData.Symbol, messageId);

        return Results.Ok(new { messageId = messageId.ToString() });
    }
    catch (RedisConnectionException ex)
    {
        logger.LogError(ex, "Redis connection failed while publishing {Symbol}", 
            marketData.Symbol);
        return Results.Problem(
            title: "Service Unavailable",
            detail: "Unable to connect to data store",
            statusCode: 503);
    }
    catch (RedisTimeoutException ex)
    {
        logger.LogError(ex, "Redis timeout while publishing {Symbol}", 
            marketData.Symbol);
        return Results.Problem(
            title: "Request Timeout",
            detail: "Data store operation timed out",
            statusCode: 504);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error processing market data for {Symbol}", 
            marketData.Symbol);
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An unexpected error occurred",
            statusCode: 500);
    }
})
.WithName("PublishMarketData");

static List<string> ValidateMarketData(MarketDataEvent data)
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

app.Run();
