# Task 08: Implement Broker Service Redis Stream Consumer

## Objective
Create the Redis Stream consumer in the Broker Service that reads market data using consumer groups.

## Prerequisites
- Task 06 completed (data flowing into Redis Streams)
- Broker.Service project configured
- StackExchange.Redis NuGet package installed

## Acceptance Criteria
- [ ] `RedisStreamConsumer.cs` BackgroundService implemented
- [ ] Consumer group pattern (XREADGROUP) working
- [ ] Message acknowledgment (XACK) implemented
- [ ] In-memory cache updated with latest prices
- [ ] Internal channel for fan-out to SSE clients
- [ ] Proper error handling for Redis operations
- [ ] Automatic consumer group creation
- [ ] Service processes all messages reliably

## Implementation

### Broker.Service/RedisStreamConsumer.cs
```csharp
using Shared.Contracts;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Channels;

namespace Broker.Service;

public class RedisStreamConsumer : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStreamConsumer> _logger;
    private readonly Channel<MarketDataEvent> _channel;
    private readonly Dictionary<string, Security> _cache = new();

    private const string StreamKey = "marketdata:stream";
    private const string ConsumerGroup = "brokers";
    private const string ConsumerName = "broker-1";

    public RedisStreamConsumer(
        IConnectionMultiplexer redis,
        ILogger<RedisStreamConsumer> logger,
        Channel<MarketDataEvent> channel)
    {
        _redis = redis;
        _logger = logger;
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        // Create consumer group if it doesn't exist
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                StreamKey, 
                ConsumerGroup, 
                StreamPosition.NewMessages);
            _logger.LogInformation("Created consumer group: {Group}", ConsumerGroup);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger.LogDebug("Consumer group already exists");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await db.StreamReadGroupAsync(
                    StreamKey,
                    ConsumerGroup,
                    ConsumerName,
                    ">",
                    count: 10);

                foreach (var entry in result)
                {
                    await ProcessMessage(db, entry, stoppingToken);
                }

                if (result.Length == 0)
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from Redis stream");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessage(
        IDatabase db, 
        StreamEntry entry, 
        CancellationToken ct)
    {
        try
        {
            var json = entry.Values[0].Value.ToString();
            var marketData = JsonSerializer.Deserialize<MarketDataEvent>(json);

            if (marketData != null)
            {
                UpdateCache(marketData);
                await _channel.Writer.WriteAsync(marketData, ct);
                
                // Acknowledge message
                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
                
                _logger.LogDebug("Processed {Symbol} @ {Price}", 
                    marketData.Symbol, marketData.Price);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}", entry.Id);
        }
    }

    private void UpdateCache(MarketDataEvent data)
    {
        if (!_cache.TryGetValue(data.Symbol, out var security))
        {
            security = new Security { Symbol = data.Symbol };
            _cache[data.Symbol] = security;
        }

        security.PreviousPrice = security.LastPrice;
        security.LastPrice = data.Price;
        security.Volume = data.Volume;
        security.LastUpdate = data.Timestamp;
    }

    public IReadOnlyDictionary<string, Security> GetAllSecurities() => _cache;
}
```

### Broker.Service/Program.cs (Partial)
```csharp
using Broker.Service;
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// Channel for broadcasting
builder.Services.AddSingleton(Channel.CreateUnbounded<MarketDataEvent>(
    new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }));

// Background consumer
builder.Services.AddHostedService<RedisStreamConsumer>();

var app = builder.Build();
app.Run();
```

## Verification
1. Run Broker Service:
   ```bash
   dotnet run --project src/Broker.Service
   ```
2. Check logs for consumer group creation
3. Verify messages are being processed
4. Check Redis consumer group info:
   ```bash
   redis-cli XINFO GROUPS marketdata:stream
   redis-cli XPENDING marketdata:stream brokers
   ```
5. Ensure no pending (unacknowledged) messages accumulate

## Related Sections
- Technical Approach Section 5.4 (Broker Service)
- Technical Approach Section 7 (Phase 3: Distribution)
- Technical Approach Section 8.3 (Redis Debugging)

## Estimated Effort
3-4 hours
