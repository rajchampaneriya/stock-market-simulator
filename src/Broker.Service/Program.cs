using Broker.Service;
using Broker.Service.Services;
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static Broker.Service.Services.SseFormatter;

var builder = WebApplication.CreateBuilder(args);

// Redis connection using Aspire service discovery
// The "cache" name matches the resource name in AppHost
builder.AddRedisClient("cache");

// Channel for candlestick events (aggregator → legacy consumers)
builder.Services.AddSingleton(Channel.CreateBounded<CandlestickEvent>(
    new BoundedChannelOptions(500)
    {
        SingleReader = false,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.DropOldest,
    }));

// Aggregator's dedicated tick channel — receives a copy of every tick
builder.Services.AddSingleton<AggregatorTickChannel>();

// Per-client SSE channel manager — each connected client gets its own bounded channels
// Fixes the multi-client bug where shared channel caused only one client to receive each tick
builder.Services.AddSingleton<SseClientManager>();

// Tick broadcaster — fans out each tick to all SSE clients + aggregator channel
builder.Services.AddSingleton<TickBroadcaster>();

// Event buffer for SSE replay support (keeps last 1000 events in memory)
var bufferSize = builder.Configuration.GetValue<int>("SSE:BufferSize", 1000);
builder.Services.AddSingleton(new MarketEventBuffer(bufferSize));

// Candlestick history — stores completed candles + seeds historical data for chart density
var historyMaxCandles = builder.Configuration.GetValue<int>("Candlestick:MaxHistory", 60);
var historySeedCount = builder.Configuration.GetValue<int>("Candlestick:SeedCount", 30);
builder.Services.AddSingleton(new CandlestickHistory(historyMaxCandles, historySeedCount));

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

// Global exception handler — prevents stack trace leaks in non-development environments
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred. Please try again later."
            });
        });
    });
}

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
// Uses per-client channels via SseClientManager for proper fan-out
app.MapGet("/api/marketdata/stream-minimal", async (
    SseClientManager clientManager,
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

    var (tickChannel, _) = clientManager.Register(clientId);

    try
    {
        // Send connection event
        await response.WriteAsync(FormatSseEvent("connected", new { clientId, timestamp = DateTime.UtcNow }), cancellationToken);
        await response.Body.FlushAsync(cancellationToken);

        // Stream market data events from per-client channel
        await foreach (var data in tickChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await response.WriteAsync(FormatSseEvent("marketdata", data), cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Minimal API: Client {ClientId} disconnected", clientId);
    }
    finally
    {
        clientManager.Unregister(clientId);
    }
})
.WithName("StreamMarketDataMinimal")
.Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

app.Run();
