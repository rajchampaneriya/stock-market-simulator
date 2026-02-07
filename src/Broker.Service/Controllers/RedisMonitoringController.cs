using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using StackExchange.Redis;
using System.Globalization;

namespace Broker.Service.Controllers;

/// <summary>
/// Controller for monitoring Redis Streams activity
/// </summary>
[ApiController]
[Route("api/redis")]
public class RedisMonitoringController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMonitoringController> _logger;
    private const string StreamKey = StreamConstants.MarketDataStream;

    public RedisMonitoringController(
        IConnectionMultiplexer redis,
        ILogger<RedisMonitoringController> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Get information about all Redis streams
    /// </summary>
    /// <returns>List of stream information</returns>
    [HttpGet("streams/info")]
    [ProducesResponseType(typeof(List<RedisStreamInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStreamsInfo()
    {
        try
        {
            var db = _redis.GetDatabase();
            var streamInfoList = new List<RedisStreamInfo>();

            // For now, we'll focus on the main marketdata stream
            // In the future, this could be extended to discover all streams
            var streamInfo = await GetStreamInfo(db, StreamKey);
            if (streamInfo != null)
            {
                streamInfoList.Add(streamInfo);
            }

            _logger.LogInformation("Retrieved info for {Count} streams", streamInfoList.Count);
            return Ok(streamInfoList);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection failed while fetching stream info");
            return StatusCode(503, new { error = "Unable to connect to Redis" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stream info");
            return StatusCode(500, new { error = "Failed to fetch stream information" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific stream
    /// </summary>
    private async Task<RedisStreamInfo?> GetStreamInfo(IDatabase db, string streamName)
    {
        try
        {
            // Check if stream exists by getting its length
            var length = await db.StreamLengthAsync(streamName);
            if (length == 0)
            {
                // Stream might not exist yet or is empty
                return new RedisStreamInfo
                {
                    StreamName = streamName,
                    MessageCount = 0,
                    LatestMessageTimestamp = null,
                    FirstEntryId = null,
                    LastEntryId = null,
                    ConsumerGroups = new List<ConsumerGroupInfo>()
                };
            }

            // Get first and last entry IDs
            var firstEntry = await db.StreamRangeAsync(streamName, minId: "-", maxId: "+", count: 1);
            var lastEntry = await db.StreamRangeAsync(streamName, minId: "-", maxId: "+", count: 1, messageOrder: Order.Descending);

            string? firstEntryId = firstEntry.FirstOrDefault().Id.ToString();
            string? lastEntryId = lastEntry.FirstOrDefault().Id.ToString();
            DateTime? latestTimestamp = null;

            // Extract timestamp from last entry ID (format: timestamp-sequence)
            if (!string.IsNullOrEmpty(lastEntryId))
            {
                var parts = lastEntryId.Split('-');
                if (parts.Length > 0 && long.TryParse(parts[0], out var timestamp))
                {
                    latestTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
                }
            }

            // Get consumer groups information
            var consumerGroups = await GetConsumerGroups(db, streamName);

            return new RedisStreamInfo
            {
                StreamName = streamName,
                MessageCount = length,
                LatestMessageTimestamp = latestTimestamp,
                FirstEntryId = firstEntryId,
                LastEntryId = lastEntryId,
                ConsumerGroups = consumerGroups
            };
        }
        catch (RedisServerException ex) when (ex.Message.Contains("no such key") || ex.Message.Contains("NOGROUP"))
        {
            // Stream or consumer group doesn't exist
            _logger.LogWarning("Stream {StreamName} does not exist or has no consumer groups", streamName);
            return new RedisStreamInfo
            {
                StreamName = streamName,
                MessageCount = 0,
                LatestMessageTimestamp = null,
                FirstEntryId = null,
                LastEntryId = null,
                ConsumerGroups = new List<ConsumerGroupInfo>()
            };
        }
    }

    /// <summary>
    /// Get consumer groups for a stream
    /// </summary>
    private async Task<List<ConsumerGroupInfo>> GetConsumerGroups(IDatabase db, string streamName)
    {
        var consumerGroups = new List<ConsumerGroupInfo>();

        try
        {
            // Use XINFO GROUPS command
            var result = await db.ExecuteAsync("XINFO", "GROUPS", streamName);
            
            if (result.IsNull)
            {
                return consumerGroups;
            }

            // Parse the result - it's an array of arrays
            var groups = (RedisResult[])result!;
            foreach (var group in groups)
            {
                var groupData = (RedisResult[])group!;
                var consumerGroupInfo = new ConsumerGroupInfo();

                // Parse group data (key-value pairs)
                for (int i = 0; i < groupData.Length; i += 2)
                {
                    var key = groupData[i].ToString();
                    var value = groupData[i + 1];

                    switch (key)
                    {
                        case "name":
                            consumerGroupInfo.GroupName = value.ToString() ?? string.Empty;
                            break;
                        case "consumers":
                            consumerGroupInfo.ConsumersCount = (int)value;
                            break;
                        case "pending":
                            consumerGroupInfo.PendingCount = (long)value;
                            break;
                        case "last-delivered-id":
                            consumerGroupInfo.LastDeliveredId = value.ToString();
                            break;
                    }
                }

                consumerGroups.Add(consumerGroupInfo);
            }
        }
        catch (RedisServerException ex) when (ex.Message.Contains("NOGROUP"))
        {
            // No consumer groups exist for this stream
            _logger.LogDebug("No consumer groups exist for stream {StreamName}", streamName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching consumer groups for stream {StreamName}", streamName);
        }

        return consumerGroups;
    }
}
