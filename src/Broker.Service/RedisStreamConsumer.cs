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
    private readonly string _consumerName;
    
    // Thread-safe for single-writer scenario (this BackgroundService is the only writer)
    // For multiple broker instances writing to shared cache, use ConcurrentDictionary
    private readonly Dictionary<string, Security> _cache = new();

    private const string StreamKey = "marketdata:stream";
    private const string ConsumerGroup = "brokers";

    public RedisStreamConsumer(
        IConnectionMultiplexer redis,
        ILogger<RedisStreamConsumer> logger,
        Channel<MarketDataEvent> channel,
        IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _channel = channel;
        
        // Generate unique consumer name for horizontal scaling
        // Uses configured prefix or default "broker", plus hostname and a GUID
        var consumerPrefix = configuration["Redis:ConsumerNamePrefix"] ?? "broker";
        var hostname = Environment.MachineName;
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Short GUID
        _consumerName = $"{consumerPrefix}-{hostname}-{uniqueId}";
        
        _logger.LogInformation("Consumer initialized with name: {ConsumerName}", _consumerName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeMessagesAsync(stoppingToken);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection lost, reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(ex, "Redis timeout, retrying in 2 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in consumer, restarting in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        await EnsureConsumerGroupExists(db);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await db.StreamReadGroupAsync(
                    StreamKey,
                    ConsumerGroup,
                    _consumerName,
                    ">",
                    count: 10);

                foreach (var entry in result)
                {
                    await ProcessMessageSafely(db, entry, stoppingToken);
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

    private async Task EnsureConsumerGroupExists(IDatabase db)
    {
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
    }

    private async Task ProcessMessageSafely(
        IDatabase db,
        StreamEntry entry,
        CancellationToken ct)
    {
        try
        {
            await ProcessMessage(db, entry, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in message {MessageId}, acknowledging to prevent reprocessing", entry.Id);
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}, will retry", entry.Id);
        }
    }

    private async Task ProcessMessage(
        IDatabase db, 
        StreamEntry entry, 
        CancellationToken ct)
    {
        if (entry.Values.Length == 0)
        {
            _logger.LogWarning("Received empty message {MessageId}", entry.Id);
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
            return;
        }

        var json = entry.Values[0].Value.ToString();
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("Received null/empty data in message {MessageId}", entry.Id);
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
            return;
        }

        var marketData = JsonSerializer.Deserialize<MarketDataEvent>(json);

        if (marketData != null)
        {
            UpdateCache(marketData);
            await _channel.Writer.WriteAsync(marketData, ct);
            
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
            
            _logger.LogDebug("Processed {Symbol} @ {Price}", 
                marketData.Symbol, marketData.Price);
        }
        else
        {
            _logger.LogWarning("Failed to deserialize message {MessageId}", entry.Id);
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
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
