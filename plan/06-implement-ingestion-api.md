# Task 06: Implement Market Ingestion API

## Objective
Create the ingestion API that receives market data via HTTP POST and publishes to Redis Streams.

## Prerequisites
- Task 02 completed (Shared.Contracts available)
- Task 03 completed (Redis configured)
- StackExchange.Redis NuGet package installed

## Acceptance Criteria
- [ ] Minimal API endpoint `/api/marketdata` created
- [ ] POST endpoint accepts MarketDataEvent JSON
- [ ] Input validation implemented
- [ ] Redis Streams publishing (XADD) working
- [ ] OpenTelemetry instrumentation added
- [ ] Health checks configured
- [ ] Returns proper HTTP status codes
- [ ] Logs all operations

## Implementation

### MarketIngestion.Api/Program.cs
```csharp
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

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

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
.WithName("PublishMarketData")
.WithOpenApi();

app.Run();
```

### MarketIngestion.Api/appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

## Verification
1. Run the ingestion API:
   ```bash
   dotnet run --project src/MarketIngestion.Api
   ```
2. Test with curl:
   ```bash
   curl -X POST http://localhost:5001/api/marketdata \
     -H "Content-Type: application/json" \
     -d '{"symbol":"TEST","price":100.50,"volume":1000,"timestamp":"2026-01-22T00:00:00Z"}'
   ```
3. Verify in Redis:
   ```bash
   redis-cli XLEN marketdata:stream
   redis-cli XRANGE marketdata:stream - + COUNT 5
   ```

## Related Sections
- Technical Approach Section 5.3 (Market Ingestion Service)
- Technical Approach Section 6.2 (Configuration)
- Technical Approach Section 8.3 (Redis Debugging)

## Estimated Effort
2-3 hours
