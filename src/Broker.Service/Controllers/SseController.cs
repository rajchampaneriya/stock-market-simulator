using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

// ReSharper disable once CheckNamespace

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/marketdata")]
public class SseController : ControllerBase
{
    private readonly Channel<MarketDataEvent> _channel;
    private readonly Channel<CandlestickEvent> _candleChannel;
    private readonly ILogger<SseController> _logger;

    public SseController(
        Channel<MarketDataEvent> channel,
        Channel<CandlestickEvent> candleChannel,
        ILogger<SseController> logger)
    {
        _channel = channel;
        _candleChannel = candleChannel;
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
        // Send connection confirmation event
        yield return FormatSseEvent(
            "connected",
            new { clientId, timestamp = DateTime.UtcNow },
            $"{clientId}-0");

        var tickReader = _channel.Reader;
        var candleReader = _candleChannel.Reader;
        var eventId = 1L;
        var lastHeartbeat = DateTime.UtcNow;

        // Multiplex both tick and candle channels into a single SSE stream.
        // We use Task.WhenAny on WaitToReadAsync to service whichever channel
        // has data first — no busy-wait spin.
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

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string FormatSseEvent(string eventType, object data, string? id = null)
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

