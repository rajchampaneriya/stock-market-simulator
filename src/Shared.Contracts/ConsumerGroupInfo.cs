namespace Shared.Contracts;

/// <summary>
/// Information about a Redis Stream consumer group
/// </summary>
public class ConsumerGroupInfo
{
    /// <summary>
    /// Name of the consumer group
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Number of consumers in this group
    /// </summary>
    public int ConsumersCount { get; set; }

    /// <summary>
    /// Number of pending (unacknowledged) messages
    /// </summary>
    public long PendingCount { get; set; }

    /// <summary>
    /// ID of the last message delivered to the consumer group
    /// </summary>
    public string? LastDeliveredId { get; set; }
}
