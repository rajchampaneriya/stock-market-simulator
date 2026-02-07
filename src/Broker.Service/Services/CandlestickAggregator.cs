using Shared.Contracts;
using System.Threading.Channels;

namespace Broker.Service.Services;

/// <summary>
/// Aggregates raw tick-level MarketDataEvents into 1-minute OHLCV candlesticks.
/// 
/// Architecture:
///   Tick Channel (MarketDataEvent) → CandlestickAggregator → Candle Channel (CandlestickEvent)
///
/// Design decisions:
///  - Runs as a BackgroundService reading from the same tick Channel.
///  - Maintains one in-progress candle per symbol.
///  - Emits candle updates at a throttled rate (configurable, default 500ms) so
///    the UI receives smooth updates without being overwhelmed.
///  - On minute boundary, emits a "completed" candle (IsComplete = true), then
///    starts a fresh candle for the new interval.
///  - A periodic timer checks for minute rollovers even when ticks are sparse.
/// </summary>
public class CandlestickAggregator : BackgroundService
{
    private readonly Channel<MarketDataEvent> _tickChannel;
    private readonly Channel<CandlestickEvent> _candleChannel;
    private readonly SseClientManager _clientManager;
    private readonly CandlestickHistory _history;
    private readonly ILogger<CandlestickAggregator> _logger;
    private readonly TimeSpan _intervalDuration;
    private readonly TimeSpan _emitThrottle;

    // Per-symbol candle state
    private readonly Dictionary<string, CandleState> _candles = new();

    public CandlestickAggregator(
        AggregatorTickChannel aggregatorTickChannel,
        Channel<CandlestickEvent> candleChannel,
        SseClientManager clientManager,
        CandlestickHistory history,
        ILogger<CandlestickAggregator> logger,
        IConfiguration configuration)
    {
        _tickChannel = aggregatorTickChannel.Channel;
        _candleChannel = candleChannel;
        _clientManager = clientManager;
        _history = history;
        _logger = logger;
        _intervalDuration = TimeSpan.FromSeconds(
            configuration.GetValue<int>("Candlestick:IntervalSeconds", 60));
        _emitThrottle = TimeSpan.FromMilliseconds(
            configuration.GetValue<int>("Candlestick:EmitThrottleMs", 500));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CandlestickAggregator started — interval {Interval}s, emit throttle {Throttle}ms",
            _intervalDuration.TotalSeconds, _emitThrottle.TotalMilliseconds);

        // Use a separate task for periodic minute-boundary checks
        var boundaryTask = CheckMinuteBoundariesAsync(stoppingToken);

        try
        {
            await foreach (var tick in _tickChannel.Reader.ReadAllAsync(stoppingToken))
            {
                ProcessTick(tick);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CandlestickAggregator stopping");
        }

        await boundaryTask;
    }

    private void ProcessTick(MarketDataEvent tick)
    {
        var intervalStart = GetIntervalStart(tick.Timestamp);

        if (!_candles.TryGetValue(tick.Symbol, out var state))
        {
            // First tick for this symbol — initialise a new candle
            state = new CandleState();
            _candles[tick.Symbol] = state;
            StartNewCandle(state, tick.Symbol, intervalStart, tick.Price, tick.Volume);
            EmitCandle(state, isComplete: false);
            return;
        }

        // Check if we've crossed into a new interval
        if (intervalStart > state.IntervalStart)
        {
            // Close the current candle
            EmitCandle(state, isComplete: true);

            // Start fresh candle for the new interval
            StartNewCandle(state, tick.Symbol, intervalStart, tick.Price, tick.Volume);
            EmitCandle(state, isComplete: false);
            return;
        }

        // Same interval — update the in-progress candle
        state.High = Math.Max(state.High, tick.Price);
        state.Low = Math.Min(state.Low, tick.Price);
        state.Close = tick.Price;
        state.Volume += tick.Volume;
        state.TickCount++;

        // Throttled emit — only send update if enough time has passed
        var now = DateTime.UtcNow;
        if (now - state.LastEmitTime >= _emitThrottle)
        {
            EmitCandle(state, isComplete: false);
            state.LastEmitTime = now;
        }
    }

    private void StartNewCandle(CandleState state, string symbol, DateTime intervalStart,
        decimal firstPrice, long firstVolume)
    {
        state.Symbol = symbol;
        state.IntervalStart = intervalStart;
        state.Open = firstPrice;
        state.High = firstPrice;
        state.Low = firstPrice;
        state.Close = firstPrice;
        state.Volume = firstVolume;
        state.TickCount = 1;
        state.LastEmitTime = DateTime.UtcNow;
    }

    private void EmitCandle(CandleState state, bool isComplete)
    {
        var candleEvent = new CandlestickEvent
        {
            Symbol = state.Symbol,
            IntervalStart = state.IntervalStart,
            Open = state.Open,
            High = state.High,
            Low = state.Low,
            Close = state.Close,
            Volume = state.Volume,
            TickCount = state.TickCount,
            IsComplete = isComplete,
        };

        // Non-blocking write — if the channel is full, drop oldest via BoundedChannelFullMode
        _candleChannel.Writer.TryWrite(candleEvent);

        // Broadcast to all connected SSE clients
        _clientManager.BroadcastCandle(candleEvent);

        // Persist completed candles for historical replay on SSE connect
        if (isComplete)
        {
            _history.Push(candleEvent);
        }

        if (isComplete)
        {
            _logger.LogDebug(
                "Candle completed: {Symbol} {Interval} O={Open} H={High} L={Low} C={Close} V={Vol} Ticks={Ticks}",
                state.Symbol, state.IntervalStart.ToString("HH:mm"),
                state.Open, state.High, state.Low, state.Close, state.Volume, state.TickCount);
        }
    }

    /// <summary>
    /// Periodic check to close candles when a minute boundary passes even without new ticks.
    /// This ensures candles are finalized promptly.
    /// </summary>
    private async Task CheckMinuteBoundariesAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.UtcNow;
                var currentInterval = GetIntervalStart(now);

                foreach (var (symbol, state) in _candles)
                {
                    if (currentInterval > state.IntervalStart && state.TickCount > 0)
                    {
                        // This candle's interval has passed — close it
                        EmitCandle(state, isComplete: true);

                        // Start a new "empty" candle carrying forward the close price
                        StartNewCandle(state, symbol, currentInterval, state.Close, 0);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    /// <summary>
    /// Truncates the timestamp to the start of its 1-minute interval.
    /// </summary>
    private DateTime GetIntervalStart(DateTime timestamp)
    {
        var intervalSeconds = (long)_intervalDuration.TotalSeconds;
        var ticks = timestamp.Ticks;
        var intervalTicks = intervalSeconds * TimeSpan.TicksPerSecond;
        var truncated = ticks - (ticks % intervalTicks);
        return new DateTime(truncated, DateTimeKind.Utc);
    }

    /// <summary>Mutable state for the current in-progress candle per symbol.</summary>
    private class CandleState
    {
        public string Symbol = string.Empty;
        public DateTime IntervalStart;
        public decimal Open;
        public decimal High;
        public decimal Low;
        public decimal Close;
        public long Volume;
        public int TickCount;
        public DateTime LastEmitTime;
    }
}
