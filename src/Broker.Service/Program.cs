using Broker.Service;
using Broker.Service.Services;
using StackExchange.Redis;
using System.Text;
using System.Threading.Channels;
using Shared.Contracts;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Redis connection using Aspire service discovery
// The "cache" name matches the resource name in AppHost
builder.AddRedisClient("cache");

// Channel for SSE tick broadcast with bounded capacity to prevent memory issues
var channelCapacity = builder.Configuration.GetValue<int>("Channel:Capacity", 1000);
builder.Services.AddSingleton(Channel.CreateBounded<MarketDataEvent>(
    new BoundedChannelOptions(channelCapacity) 
    { 
        SingleReader = false, 
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest // Drop oldest messages on backpressure
    }));

// Channel for candlestick events (aggregator → SSE)
builder.Services.AddSingleton(Channel.CreateBounded<CandlestickEvent>(
    new BoundedChannelOptions(500)
    {
        SingleReader = false,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.DropOldest,
    }));

// Aggregator's dedicated tick channel — receives a copy of every tick
builder.Services.AddSingleton<AggregatorTickChannel>();

// Tick broadcaster — fans out each tick to SSE channel + aggregator channel
builder.Services.AddSingleton<TickBroadcaster>();

// Event buffer for SSE replay support (keeps last 1000 events in memory)
var bufferSize = builder.Configuration.GetValue<int>("SSE:BufferSize", 1000);
builder.Services.AddSingleton(new MarketEventBuffer(bufferSize));

// Background consumer (reads from Redis stream)
builder.Services.AddHostedService<RedisStreamConsumer>();

// Candlestick aggregation pipeline (tick → OHLCV candles)
builder.Services.AddHostedService<CandlestickAggregator>();

// Health checks
var redisConnection = builder.Configuration.GetConnectionString("cache") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection);

// CORS for browser access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins for Aspire-generated URLs
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // In production, use strict origin configuration
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:5000", "http://localhost:5173" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Broker.Service",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddRedisInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapHealthChecks("/health");
app.MapControllers();

// Minimal API endpoint for SSE - demonstrates clean .NET 10 streaming pattern
// Uses IAsyncEnumerable for composable, efficient event streaming
app.MapGet("/api/marketdata/stream-minimal", async (
    Channel<MarketDataEvent> channel,
    ILogger<Program> logger,
    HttpResponse response,
    CancellationToken cancellationToken) =>
{
    var clientId = Guid.NewGuid().ToString("N");
    logger.LogInformation("Minimal API: Client {ClientId} connected", clientId);

    // Configure SSE response headers
    response.Headers.Append("Content-Type", "text/event-stream");
    response.Headers.Append("Cache-Control", "no-cache");
    response.Headers.Append("Connection", "keep-alive");

    try
    {
        // Send connection event
        await response.WriteAsync(FormatSseEvent("connected", new { clientId, timestamp = DateTime.UtcNow }), cancellationToken);
        await response.Body.FlushAsync(cancellationToken);

        // Stream market data events using IAsyncEnumerable
        await foreach (var data in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await response.WriteAsync(FormatSseEvent("marketdata", data), cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Minimal API: Client {ClientId} disconnected", clientId);
    }
})
.WithName("StreamMarketDataMinimal")
.Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

app.Run();

static string FormatSseEvent(string eventType, object data, string? id = null)
{
    var options = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
    var sb = new StringBuilder();
    
    if (!string.IsNullOrEmpty(id))
    {
        sb.AppendLine($"id: {id}");
    }
    
    sb.AppendLine($"event: {eventType}");
    sb.AppendLine($"data: {System.Text.Json.JsonSerializer.Serialize(data, options)}");
    sb.AppendLine();
    
    return sb.ToString();
}
