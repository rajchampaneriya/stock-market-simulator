using Shared.Contracts;
using System.Threading.Channels;

namespace Broker.Service.Services;

/// <summary>
/// Typed wrapper for the channel dedicated to the CandlestickAggregator.
/// Allows DI to distinguish the aggregator's tick channel from the SSE tick channel.
/// </summary>
public class AggregatorTickChannel
{
    public Channel<MarketDataEvent> Channel { get; }

    public AggregatorTickChannel(int capacity = 2000)
    {
        Channel = System.Threading.Channels.Channel.CreateBounded<MarketDataEvent>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest,
            });
    }
}

/// <summary>
/// Broadcasts each MarketDataEvent to multiple consumers:
///  1. Per-client SSE channels via SseClientManager (for watchlist / banner / real-time ticks)
///  2. The CandlestickAggregator (for OHLCV candle construction)
///
/// This replaces direct single-channel writes, enabling fan-out without
/// losing messages to either consumer. Each connected SSE client receives
/// every tick through its own dedicated bounded channel.
/// </summary>
public class TickBroadcaster
{
    private readonly SseClientManager _clientManager;
    private readonly Channel<MarketDataEvent> _aggregatorChannel;
    private readonly ILogger<TickBroadcaster> _logger;

    public TickBroadcaster(
        SseClientManager clientManager,
        AggregatorTickChannel aggregatorTickChannel,
        ILogger<TickBroadcaster> logger)
    {
        _clientManager = clientManager;
        _aggregatorChannel = aggregatorTickChannel.Channel;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a tick to all downstream consumers.
    /// Uses TryWrite (non-blocking) — bounded channels drop oldest if full.
    /// </summary>
    public void Broadcast(MarketDataEvent tick)
    {
        // Fan out to all connected SSE clients
        _clientManager.BroadcastTick(tick);

        if (!_aggregatorChannel.Writer.TryWrite(tick))
        {
            _logger.LogWarning("Aggregator channel full, tick for {Symbol} dropped", tick.Symbol);
        }
    }
}
