namespace Shared.Contracts;

/// <summary>
/// Represents a candlestick (OHLCV) aggregated from tick data.
/// Produced by the backend CandlestickAggregator and streamed to clients via SSE.
/// </summary>
public record CandlestickEvent
{
    /// <summary>Security symbol (e.g., "NIFTY50").</summary>
    public string Symbol { get; init; } = string.Empty;
    
    /// <summary>Start of the candle interval (UTC).</summary>
    public DateTime IntervalStart { get; init; }
    
    /// <summary>Opening price at the start of the interval.</summary>
    public decimal Open { get; init; }
    
    /// <summary>Highest price during the interval.</summary>
    public decimal High { get; init; }
    
    /// <summary>Lowest price during the interval.</summary>
    public decimal Low { get; init; }
    
    /// <summary>Most recent price (close of the interval or in-progress close).</summary>
    public decimal Close { get; init; }
    
    /// <summary>Cumulative volume during the interval.</summary>
    public long Volume { get; init; }
    
    /// <summary>Number of ticks aggregated into this candle.</summary>
    public int TickCount { get; init; }
    
    /// <summary>
    /// True when this candle is still accumulating ticks (the current minute hasn't ended).
    /// False when the candle is finalised and a new interval has started.
    /// </summary>
    public bool IsComplete { get; init; }
}
