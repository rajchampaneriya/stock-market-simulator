using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Broker.Service.Services;

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/marketdata")]
public class SseControllerV2 : ControllerBase
{
    private readonly Channel<MarketDataEvent> _channel;
    private readonly MarketEventBuffer _eventBuffer;
    private readonly ILogger<SseControllerV2> _logger;

    public SseControllerV2(
        Channel<MarketDataEvent> channel,
        MarketEventBuffer eventBuffer,
        ILogger<SseControllerV2> logger)
    {
        _channel = channel;
        _eventBuffer = eventBuffer;
        _logger = logger;
    }

    /// <summary>
    /// Stream real-time market data with automatic event replay on reconnection.
    /// Supports Last-Event-ID header for missed event recovery following SSE specification.
    /// </summary>
    [HttpGet("stream-with-replay")]
    public async Task StreamWithReplay(
        [FromHeader(Name = "Last-Event-ID")] string? lastEventId,
        CancellationToken cancellationToken)
    {
        // Configure SSE response headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var clientId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Client {ClientId} connected. Last-Event-ID: {LastEventId}",
            clientId,
            lastEventId ?? "none");

        try
        {
            // Stream events with replay support using IAsyncEnumerable pattern
            await foreach (var sseEvent in StreamEventsWithReplayAsync(clientId, lastEventId, cancellationToken))
            {
                await Response.WriteAsync(sseEvent, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client {ClientId} disconnected", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming to client {ClientId}", clientId);
        }
    }

    private async IAsyncEnumerable<string> StreamEventsWithReplayAsync(
        string clientId,
        string? lastEventId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Send connection confirmation
        yield return FormatSseEvent(
            "connected",
            new { clientId, timestamp = DateTime.UtcNow, reconnected = !string.IsNullOrWhiteSpace(lastEventId) },
            $"conn-{clientId}");

        // 2. Replay missed events if Last-Event-ID is provided
        if (!string.IsNullOrWhiteSpace(lastEventId))
        {
            var missedEvents = _eventBuffer.GetEventsAfter(lastEventId);
            var missedCount = 0;

            foreach (var missedEvent in missedEvents)
            {
                yield return FormatSseEvent(
                    missedEvent.EventType ?? "marketdata",
                    missedEvent.Data,
                    missedEvent.Id);
                missedCount++;
            }

            if (missedCount > 0)
            {
                _logger.LogInformation(
                    "Replayed {Count} missed events to client {ClientId}",
                    missedCount,
                    clientId);
            }
        }

        // 3. Stream new events from the channel
        var reader = _channel.Reader;
        await foreach (var marketData in reader.ReadAllAsync(cancellationToken))
        {
            // Add to buffer and get the event with ID
            var bufferedEvent = _eventBuffer.Add(marketData);

            yield return FormatSseEvent(
                bufferedEvent.EventType ?? "marketdata",
                marketData,
                bufferedEvent.Id);
        }
    }

    private static string FormatSseEvent(string eventType, object data, string? id = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(id))
        {
            sb.AppendLine($"id: {id}");
        }

        sb.AppendLine($"event: {eventType}");
        sb.AppendLine($"data: {JsonSerializer.Serialize(data)}");
        sb.AppendLine(); // Empty line signals end of event

        return sb.ToString();
    }
}
