namespace Shared.Contracts;

/// <summary>
/// Information about a Redis Stream
/// </summary>
public class RedisStreamInfo
{
    /// <summary>
    /// Name of the Redis stream
    /// </summary>
    public string StreamName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of messages in the stream
    /// </summary>
    public long MessageCount { get; set; }

    /// <summary>
    /// Timestamp of the latest message (extracted from message ID)
    /// </summary>
    public DateTime? LatestMessageTimestamp { get; set; }

    /// <summary>
    /// ID of the first entry in the stream
    /// </summary>
    public string? FirstEntryId { get; set; }

    /// <summary>
    /// ID of the last entry in the stream
    /// </summary>
    public string? LastEntryId { get; set; }

    /// <summary>
    /// List of consumer groups for this stream
    /// </summary>
    public List<ConsumerGroupInfo> ConsumerGroups { get; set; } = new();
}
