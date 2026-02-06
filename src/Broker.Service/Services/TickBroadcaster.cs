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
///  1. The SSE tick channel (for watchlist / banner / real-time ticks)
///  2. The CandlestickAggregator (for OHLCV candle construction)
///
/// This replaces direct single-channel writes, enabling fan-out without
/// losing messages to either consumer.
/// </summary>
public class TickBroadcaster
{
    private readonly Channel<MarketDataEvent> _sseChannel;
    private readonly Channel<MarketDataEvent> _aggregatorChannel;
    private readonly ILogger<TickBroadcaster> _logger;

    public TickBroadcaster(
        Channel<MarketDataEvent> sseChannel,
        AggregatorTickChannel aggregatorTickChannel,
        ILogger<TickBroadcaster> logger)
    {
        _sseChannel = sseChannel;
        _aggregatorChannel = aggregatorTickChannel.Channel;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a tick to all downstream consumers.
    /// Uses TryWrite (non-blocking) — bounded channels drop oldest if full.
    /// </summary>
    public void Broadcast(MarketDataEvent tick)
    {
        if (!_sseChannel.Writer.TryWrite(tick))
        {
            _logger.LogWarning("SSE channel full, tick for {Symbol} dropped", tick.Symbol);
        }

        if (!_aggregatorChannel.Writer.TryWrite(tick))
        {
            _logger.LogWarning("Aggregator channel full, tick for {Symbol} dropped", tick.Symbol);
        }
    }
}
