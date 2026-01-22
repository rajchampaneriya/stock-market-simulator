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
