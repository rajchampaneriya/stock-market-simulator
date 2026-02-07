using Microsoft.AspNetCore.Mvc;
using Broker.Service.Services;
using static Broker.Service.Services.SseFormatter;

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/marketdata")]
public class SseControllerV2 : ControllerBase
{
    private readonly SseClientManager _clientManager;
    private readonly MarketEventBuffer _eventBuffer;
    private readonly CandlestickHistory _history;
    private readonly ILogger<SseControllerV2> _logger;

    public SseControllerV2(
        SseClientManager clientManager,
        MarketEventBuffer eventBuffer,
        CandlestickHistory history,
        ILogger<SseControllerV2> logger)
    {
        _clientManager = clientManager;
        _eventBuffer = eventBuffer;
        _history = history;
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
        // Register per-client channels for fan-out broadcasting
        var (tickChannel, candleChannel) = _clientManager.Register(clientId);

        try
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
            else
            {
                // Replay historical candles on first connect (like V1)
                var allHistory = _history.GetAllHistory();
                var historyCount = 0;
                foreach (var (symbol, candles) in allHistory)
                {
                    foreach (var candle in candles)
                    {
                        yield return FormatSseEvent(
                            "candlestick",
                            candle,
                            $"{clientId}-h{historyCount++}");
                    }
                }

                if (historyCount > 0)
                {
                    _logger.LogInformation(
                        "Sent {Count} historical candles to client {ClientId}",
                        historyCount, clientId);
                }
            }

            // 3. Stream new events — multiplex tick + candle channels (like V1)
            var tickReader = tickChannel.Reader;
            var candleReader = candleChannel.Reader;
            var lastHeartbeat = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                var tickWait = tickReader.WaitToReadAsync(cancellationToken).AsTask();
                var candleWait = candleReader.WaitToReadAsync(cancellationToken).AsTask();

                await Task.WhenAny(tickWait, candleWait);

                // Drain all available ticks
                while (tickReader.TryRead(out var marketData))
                {
                    var bufferedEvent = _eventBuffer.Add(marketData);
                    yield return FormatSseEvent(
                        bufferedEvent.EventType ?? "marketdata",
                        marketData,
                        bufferedEvent.Id);
                }

                // Drain all available candle events
                while (candleReader.TryRead(out var candleEvent))
                {
                    yield return FormatSseEvent(
                        "candlestick",
                        candleEvent,
                        $"{clientId}-c{_eventBuffer.CurrentEventId}");
                }

                // Send heartbeat every 15 seconds
                if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= 15)
                {
                    yield return FormatSseEvent(
                        "heartbeat",
                        new { timestamp = DateTime.UtcNow },
                        $"{clientId}-heartbeat");
                    lastHeartbeat = DateTime.UtcNow;
                }
            }
        }
        finally
        {
            _clientManager.Unregister(clientId);
        }
    }

}
