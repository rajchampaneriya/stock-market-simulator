namespace Shared.Contracts;

/// <summary>
/// Represents a market data tick event.
/// Simple model for MVP - symbol, price, volume, timestamp only.
/// </summary>
public record MarketDataEvent
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
}
