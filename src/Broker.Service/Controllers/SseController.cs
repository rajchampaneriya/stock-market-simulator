using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using System.Text.Json;
using System.Threading.Channels;

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/marketdata")]
public class SseController : ControllerBase
{
    private readonly Channel<MarketDataEvent> _channel;
    private readonly ILogger<SseController> _logger;

    public SseController(
        Channel<MarketDataEvent> channel,
        ILogger<SseController> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var clientId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Client {ClientId} connected", clientId);

        try
        {
            // Send connection confirmation
            await WriteEvent("connected", 
                new { clientId, timestamp = DateTime.UtcNow }, 
                cancellationToken);

            var reader = _channel.Reader;
            using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            var readTask = ReadChannelAsync(reader, clientId, cancellationToken);
            var heartbeatTask = SendHeartbeatsAsync(heartbeatTimer, cancellationToken);

            await Task.WhenAny(readTask, heartbeatTask);
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

    private async Task ReadChannelAsync(
        ChannelReader<MarketDataEvent> reader,
        string clientId,
        CancellationToken ct)
    {
        await foreach (var marketData in reader.ReadAllAsync(ct))
        {
            await WriteEvent("marketdata", marketData, ct);
        }
    }

    private async Task SendHeartbeatsAsync(
        PeriodicTimer timer,
        CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            await WriteEvent("heartbeat", 
                new { timestamp = DateTime.UtcNow }, 
                ct);
        }
    }

    private async Task WriteEvent(
        string eventName, 
        object data, 
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: {eventName}\n", ct);
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
