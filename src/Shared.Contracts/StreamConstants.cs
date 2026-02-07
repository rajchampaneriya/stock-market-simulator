namespace Shared.Contracts;

/// <summary>
/// Centralized constants for Redis stream keys and consumer group names.
/// Eliminates hardcoded string duplication across services.
/// </summary>
public static class StreamConstants
{
    /// <summary>
    /// The Redis stream key used for market data events.
    /// Used by MarketIngestion.Api (publisher) and Broker.Service (consumer).
    /// </summary>
    public const string MarketDataStream = "marketdata:stream";

    /// <summary>
    /// The consumer group name for broker service instances.
    /// </summary>
    public const string BrokerConsumerGroup = "brokers";
}
