using Microsoft.AspNetCore.Mvc;
using Broker.Service.Services;
using static Broker.Service.Services.SseFormatter;

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/marketdata")]
public class SseController : ControllerBase
{
    private readonly SseClientManager _clientManager;
    private readonly CandlestickHistory _history;
    private readonly ILogger<SseController> _logger;

    public SseController(
        SseClientManager clientManager,
        CandlestickHistory history,
        ILogger<SseController> logger)
    {
        _clientManager = clientManager;
        _history = history;
        _logger = logger;
    }

    /// <summary>
    /// Stream real-time market data using Server-Sent Events.
    /// Leverages IAsyncEnumerable pattern for clean, composable streaming.
    /// </summary>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        // Configure SSE response headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

        var clientId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Client {ClientId} connected for SSE stream", clientId);

        try
        {
            // Stream events using IAsyncEnumerable pattern
            await foreach (var sseEvent in StreamEventsAsync(clientId, cancellationToken))
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

    private async IAsyncEnumerable<string> StreamEventsAsync(
        string clientId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Register per-client channels for fan-out broadcasting
        var (tickChannel, candleChannel) = _clientManager.Register(clientId);

        try
        {
            // Send connection confirmation event
            yield return FormatSseEvent(
                "connected",
                new { clientId, timestamp = DateTime.UtcNow },
                $"{clientId}-0");

            // Replay historical candles so chart has density immediately
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

            var tickReader = tickChannel.Reader;
            var candleReader = candleChannel.Reader;
            var eventId = 1L;
            var lastHeartbeat = DateTime.UtcNow;

            // Multiplex both tick and candle channels into a single SSE stream.
            while (!cancellationToken.IsCancellationRequested)
            {
                var tickWait = tickReader.WaitToReadAsync(cancellationToken).AsTask();
                var candleWait = candleReader.WaitToReadAsync(cancellationToken).AsTask();

                await Task.WhenAny(tickWait, candleWait);

                // Drain all available ticks
                while (tickReader.TryRead(out var marketData))
                {
                    yield return FormatSseEvent(
                        "marketdata",
                        marketData,
                        $"{clientId}-{eventId++}");
                }

                // Drain all available candle events
                while (candleReader.TryRead(out var candleEvent))
                {
                    yield return FormatSseEvent(
                        "candlestick",
                        candleEvent,
                        $"{clientId}-c{eventId++}");
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

