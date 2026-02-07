using Shared.Contracts;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Broker.Service.Services;

/// <summary>
/// Manages per-client SSE channels for fan-out broadcasting.
/// 
/// Problem solved: The previous architecture used a single Channel&lt;MarketDataEvent&gt; shared
/// across all SSE clients. Since channel reads are destructive (each item consumed by one reader),
/// only one SSE client would receive each tick.
/// 
/// Solution: Each SSE client gets its own bounded channel. The TickBroadcaster writes to all
/// registered client channels, ensuring every connected client receives every tick.
/// </summary>
public class SseClientManager
{
    private readonly ConcurrentDictionary<string, Channel<MarketDataEvent>> _clients = new();
    private readonly ConcurrentDictionary<string, Channel<CandlestickEvent>> _candleClients = new();
    private readonly int _channelCapacity;
    private readonly ILogger<SseClientManager> _logger;

    public SseClientManager(ILogger<SseClientManager> logger, IConfiguration configuration)
    {
        _channelCapacity = configuration.GetValue<int>("Channel:ClientCapacity", 500);
        _logger = logger;
    }

    /// <summary>
    /// Registers a new SSE client and returns dedicated tick and candle channels.
    /// </summary>
    public (Channel<MarketDataEvent> TickChannel, Channel<CandlestickEvent> CandleChannel) Register(string clientId)
    {
        var tickChannel = Channel.CreateBounded<MarketDataEvent>(
            new BoundedChannelOptions(_channelCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

        var candleChannel = Channel.CreateBounded<CandlestickEvent>(
            new BoundedChannelOptions(_channelCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

        _clients[clientId] = tickChannel;
        _candleClients[clientId] = candleChannel;

        _logger.LogInformation("SSE client {ClientId} registered. Active clients: {Count}",
            clientId, _clients.Count);

        return (tickChannel, candleChannel);
    }

    /// <summary>
    /// Removes a client when they disconnect.
    /// </summary>
    public void Unregister(string clientId)
    {
        if (_clients.TryRemove(clientId, out var tickChannel))
        {
            tickChannel.Writer.TryComplete();
        }

        if (_candleClients.TryRemove(clientId, out var candleChannel))
        {
            candleChannel.Writer.TryComplete();
        }

        _logger.LogInformation("SSE client {ClientId} unregistered. Active clients: {Count}",
            clientId, _clients.Count);
    }

    /// <summary>
    /// Broadcasts a tick event to all connected SSE clients.
    /// </summary>
    public void BroadcastTick(MarketDataEvent tick)
    {
        foreach (var (clientId, channel) in _clients)
        {
            if (!channel.Writer.TryWrite(tick))
            {
                _logger.LogDebug("Client {ClientId} tick channel full, oldest tick dropped", clientId);
            }
        }
    }

    /// <summary>
    /// Broadcasts a candlestick event to all connected SSE clients.
    /// </summary>
    public void BroadcastCandle(CandlestickEvent candle)
    {
        foreach (var (clientId, channel) in _candleClients)
        {
            if (!channel.Writer.TryWrite(candle))
            {
                _logger.LogDebug("Client {ClientId} candle channel full, oldest candle dropped", clientId);
            }
        }
    }

    /// <summary>
    /// Gets the count of active connected clients.
    /// </summary>
    public int ActiveClientCount => _clients.Count;
}
