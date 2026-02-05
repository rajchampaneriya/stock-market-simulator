# SSE Quick Reference - .NET 10 Implementation

## Three Ways to Use SSE in This Project

### 1️⃣ Basic Streaming (Recommended for most use cases)
```csharp
// Endpoint: GET /api/marketdata/stream
// File: src/Broker.Service/Controllers/SseController.cs

[HttpGet("stream")]
public async Task Stream(CancellationToken cancellationToken)
{
    Response.Headers.Append("Content-Type", "text/event-stream");
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");
    
    await foreach (var sseEvent in StreamEventsAsync(clientId, cancellationToken))
    {
        await Response.WriteAsync(sseEvent, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

private async IAsyncEnumerable<string> StreamEventsAsync(...)
{
    yield return FormatSseEvent("connected", data, id);
    
    await foreach (var marketData in channel.Reader.ReadAllAsync(ct))
    {
        yield return FormatSseEvent("marketdata", marketData, id);
    }
}
```

### 2️⃣ With Event Replay (Recommended for production)
```csharp
// Endpoint: GET /api/marketdata/stream-with-replay
// File: src/Broker.Service/Controllers/SseControllerV2.cs

[HttpGet("stream-with-replay")]
public async Task StreamWithReplay(
    [FromHeader(Name = "Last-Event-ID")] string? lastEventId,
    CancellationToken cancellationToken)
{
    // 1. Send connection event
    yield return FormatSseEvent("connected", ...);
    
    // 2. Replay missed events
    if (!string.IsNullOrWhiteSpace(lastEventId))
    {
        var missedEvents = eventBuffer.GetEventsAfter(lastEventId);
        foreach (var missed in missedEvents)
            yield return FormatSseEvent(...);
    }
    
    // 3. Stream new events
    await foreach (var data in channel.Reader.ReadAllAsync(ct))
    {
        var bufferedEvent = eventBuffer.Add(data);
        yield return FormatSseEvent("marketdata", data, bufferedEvent.Id);
    }
}
```

### 3️⃣ Minimal API (Simplest implementation)
```csharp
// Endpoint: GET /api/marketdata/stream-minimal
// File: src/Broker.Service/Program.cs

app.MapGet("/api/marketdata/stream-minimal", async (
    Channel<MarketDataEvent> channel,
    HttpResponse response,
    CancellationToken ct) =>
{
    response.Headers.Append("Content-Type", "text/event-stream");
    response.Headers.Append("Cache-Control", "no-cache");
    
    await response.WriteAsync(FormatSseEvent("connected", ...));
    await response.Body.FlushAsync(ct);
    
    await foreach (var data in channel.Reader.ReadAllAsync(ct))
    {
        await response.WriteAsync(FormatSseEvent("marketdata", data));
        await response.Body.FlushAsync(ct);
    }
});
```

## SSE Event Format Helper

```csharp
private static string FormatSseEvent(string eventType, object data, string? id = null)
{
    var sb = new StringBuilder();
    
    if (!string.IsNullOrEmpty(id))
        sb.AppendLine($"id: {id}");
    
    sb.AppendLine($"event: {eventType}");
    sb.AppendLine($"data: {JsonSerializer.Serialize(data)}");
    sb.AppendLine(); // Empty line = end of event
    
    return sb.ToString();
}
```

**Output Example:**
```
id: client123-1
event: marketdata
data: {"symbol":"AAPL","price":150.25,"volume":1000,"timestamp":"2026-02-05T10:30:00Z"}

```

## React Frontend Integration

```typescript
// File: src/frontend/src/hooks/useMarketData.ts

const eventSource = new EventSource(
    `${brokerUrl}/api/marketdata/stream`
);

// Connection established
eventSource.addEventListener('connected', (e) => {
    const data = JSON.parse(e.data);
    console.log('Connected:', data.clientId);
});

// Market data received
eventSource.addEventListener('marketdata', (e) => {
    const marketData: MarketDataEvent = JSON.parse(e.data);
    updateSecurity(marketData);
});

// Heartbeat (keep-alive)
eventSource.addEventListener('heartbeat', (e) => {
    console.debug('Heartbeat:', JSON.parse(e.data).timestamp);
});

// Error handling (automatic reconnection)
eventSource.onerror = (e) => {
    if (eventSource.readyState === EventSource.CLOSED) {
        console.error('Connection lost, reconnecting...');
    }
};

// Cleanup
return () => eventSource.close();
```

## Testing Commands

```bash
# Test basic stream
curl -N http://localhost:5001/api/marketdata/stream

# Test with replay (simulating reconnection)
curl -N -H "Last-Event-ID: 42" \
  http://localhost:5001/api/marketdata/stream-with-replay

# Test minimal API
curl -N http://localhost:5001/api/marketdata/stream-minimal

# Monitor in browser DevTools
# Network tab → Filter: "EventStream"
# Console: See connection logs
```

## Key Configuration

**File**: `src/Broker.Service/Program.cs`

```csharp
// Channel capacity (events before dropping oldest)
var channelCapacity = builder.Configuration
    .GetValue<int>("Channel:Capacity", 1000);

// Event buffer size (for replay)
var bufferSize = builder.Configuration
    .GetValue<int>("SSE:BufferSize", 1000);
```

**File**: `appsettings.json`

```json
{
  "Channel": {
    "Capacity": 1000
  },
  "SSE": {
    "BufferSize": 1000
  }
}
```

## Common Patterns

### Pattern 1: IAsyncEnumerable Streaming
```csharp
private async IAsyncEnumerable<string> StreamData(
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var item in source.ReadAllAsync(ct))
    {
        yield return FormatSseEvent("event-type", item);
    }
}
```

### Pattern 2: Adding Event IDs
```csharp
var eventId = 1L;

await foreach (var item in source.ReadAllAsync(ct))
{
    yield return FormatSseEvent("event-type", item, $"id-{eventId++}");
}
```

### Pattern 3: Periodic Heartbeats
```csharp
var lastHeartbeat = DateTime.UtcNow;

await foreach (var item in source.ReadAllAsync(ct))
{
    yield return FormatSseEvent("data", item);
    
    if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= 15)
    {
        yield return FormatSseEvent("heartbeat", new { timestamp = DateTime.UtcNow });
        lastHeartbeat = DateTime.UtcNow;
    }
}
```

## Required HTTP Headers

```csharp
Response.Headers.Append("Content-Type", "text/event-stream");
Response.Headers.Append("Cache-Control", "no-cache");
Response.Headers.Append("Connection", "keep-alive");
Response.Headers.Append("X-Accel-Buffering", "no"); // For nginx
```

## Event Types in This Project

| Event Type | Purpose | Data |
|------------|---------|------|
| `connected` | Connection confirmation | `{ clientId, timestamp }` |
| `marketdata` | Real-time market data | `MarketDataEvent` object |
| `heartbeat` | Keep connection alive | `{ timestamp }` |

## MarketDataEvent Structure

```csharp
public record MarketDataEvent
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
}
```

## Troubleshooting

### Events not streaming?
1. Check Channel has data: `channel.Reader.Count`
2. Verify headers: `Content-Type: text/event-stream`
3. Check CORS configuration
4. Ensure connection not buffered (X-Accel-Buffering: no)

### Reconnection not working?
1. Verify event IDs are being sent
2. Check browser sends `Last-Event-ID` header
3. Verify `MarketEventBuffer` is registered in DI
4. Check buffer has the missed events

### High memory usage?
1. Reduce Channel capacity
2. Reduce SSE buffer size
3. Check for event leaks (events not being consumed)

## Production Checklist

- [ ] Add authentication (`[Authorize]` attribute)
- [ ] Configure CORS with specific origins
- [ ] Add rate limiting per client
- [ ] Monitor connection counts
- [ ] Set up logging for diagnostics
- [ ] Configure buffer sizes based on load testing
- [ ] Add health checks for SSE endpoints
- [ ] Set up sticky sessions (if load balanced)
- [ ] Add per-user event filtering (if needed)

## Resources

- **SSE Specification**: https://html.spec.whatwg.org/multipage/server-sent-events.html
- **EventSource API**: https://developer.mozilla.org/en-US/docs/Web/API/EventSource
- **IAsyncEnumerable**: https://docs.microsoft.com/en-us/dotnet/csharp/async#streams

---

**Quick Start**: Use SseController for basic streaming, SseControllerV2 for production with replay support.
