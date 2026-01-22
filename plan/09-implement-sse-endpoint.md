# Task 09: Implement Server-Sent Events (SSE) Endpoint

## Objective
Create the SSE endpoint in Broker Service that streams market data to browser clients.

## Prerequisites
- Task 08 completed (Redis consumer working)
- Channel<MarketDataEvent> configured for broadcasting

## Acceptance Criteria
- [ ] SSE endpoint `/api/marketdata/stream` created
- [ ] Proper SSE headers set (Content-Type: text/event-stream)
- [ ] EventSource protocol implemented correctly
- [ ] Heartbeat mechanism for connection keep-alive
- [ ] Multiple clients supported simultaneously
- [ ] Graceful connection cleanup on client disconnect
- [ ] CORS configured for browser access
- [ ] Connection management implemented

## Implementation

### Broker.Service/Controllers/SseController.cs
```csharp
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
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        var clientId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("Client {ClientId} connected", clientId);

        try
        {
            // Send connection confirmation
            await WriteEvent("connected", 
                new { clientId, timestamp = DateTime.UtcNow }, 
                cancellationToken);

            var reader = _channel.Reader;
            var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
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
```

### Broker.Service/Program.cs (Update)
```csharp
// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();
app.MapControllers();
```

## Verification

### 1. Test with curl
```bash
curl -N http://localhost:5002/api/marketdata/stream
```

Expected output:
```
event: connected
data: {"clientId":"abc123","timestamp":"2026-01-22T01:30:00Z"}

event: marketdata
data: {"symbol":"NIFTY50","price":1001.23,"volume":5432,"timestamp":"2026-01-22T01:30:01Z"}

event: heartbeat
data: {"timestamp":"2026-01-22T01:30:15Z"}
```

### 2. Test with JavaScript
```javascript
const eventSource = new EventSource('http://localhost:5002/api/marketdata/stream');

eventSource.addEventListener('connected', (e) => {
  console.log('Connected:', JSON.parse(e.data));
});

eventSource.addEventListener('marketdata', (e) => {
  console.log('Market data:', JSON.parse(e.data));
});

eventSource.addEventListener('heartbeat', (e) => {
  console.log('Heartbeat:', JSON.parse(e.data));
});
```

### 3. Test Multiple Clients
- Open 2-3 browser tabs with the same EventSource connection
- Verify all receive the same data
- Close one tab, verify others continue working

### 4. Test Reconnection
- Stop Broker Service
- Verify browser auto-reconnects when service restarts

## Related Sections
- Technical Approach Section 5.4 (Broker Service - SSE)
- Technical Approach Section 7 (Phase 3: Distribution)
- Technical Approach Section 10.1 (Why SSE?)

## Estimated Effort
2-3 hours
