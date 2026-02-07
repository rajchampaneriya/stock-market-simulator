using System.Text;
using System.Text.Json;

namespace Broker.Service.Services;

/// <summary>
/// Shared SSE event formatter. Consolidates the FormatSseEvent logic
/// that was previously duplicated across SseController, SseControllerV2, and Program.cs.
/// </summary>
public static class SseFormatter
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Formats an object as an SSE event string following the Server-Sent Events specification.
    /// </summary>
    /// <param name="eventType">The SSE event type (e.g., "marketdata", "candlestick", "heartbeat").</param>
    /// <param name="data">The data payload to serialize as JSON.</param>
    /// <param name="id">Optional event ID for Last-Event-ID reconnection support.</param>
    /// <returns>A properly formatted SSE event string.</returns>
    public static string FormatSseEvent(string eventType, object data, string? id = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(id))
        {
            sb.AppendLine($"id: {id}");
        }

        sb.AppendLine($"event: {eventType}");
        sb.AppendLine($"data: {JsonSerializer.Serialize(data, CamelCaseOptions)}");
        sb.AppendLine(); // Empty line signals end of event

        return sb.ToString();
    }
}
