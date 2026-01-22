# Task 14: Improve Error Handling

## Objective
Enhance error handling across all services for better resilience and debugging.

## Prerequisites
- All core services implemented (Tasks 05-09 completed)

## Acceptance Criteria
- [ ] Structured error handling in Market Simulator
- [ ] Retry logic with exponential backoff for HTTP calls
- [ ] Circuit breaker pattern for Redis connections
- [ ] Graceful degradation when services are unavailable
- [ ] Comprehensive error logging with context
- [ ] User-friendly error messages in UI
- [ ] Dead letter queue for failed messages
- [ ] Health check endpoints return meaningful status

## Implementation Areas

### 1. Market Simulator Error Handling
```csharp
private async Task SendToIngestion(MarketDataEvent data, CancellationToken ct)
{
    const int maxRetries = 3;
    var retryCount = 0;
    var delay = TimeSpan.FromMilliseconds(100);

    while (retryCount < maxRetries)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/marketdata", data, ct);
            response.EnsureSuccessStatusCode();
            return; // Success
        }
        catch (HttpRequestException ex)
        {
            retryCount++;
            _logger.LogWarning(ex, 
                "Failed to send {Symbol} (attempt {Attempt}/{Max})", 
                data.Symbol, retryCount, maxRetries);
            
            if (retryCount >= maxRetries)
            {
                _logger.LogError("Max retries reached for {Symbol}", data.Symbol);
                return;
            }
            
            await Task.Delay(delay, ct);
            delay *= 2; // Exponential backoff
        }
    }
}
```

### 2. Ingestion API Error Handling
```csharp
app.MapPost("/api/marketdata", async (
    [FromBody] MarketDataEvent marketData,
    IConnectionMultiplexer redis,
    ILogger<Program> logger) =>
{
    try
    {
        // Enhanced validation
        var validationErrors = ValidateMarketData(marketData);
        if (validationErrors.Any())
        {
            logger.LogWarning("Validation failed: {Errors}", 
                string.Join(", ", validationErrors));
            return Results.ValidationProblem(
                validationErrors.ToDictionary(e => e, e => new[] { e }));
        }

        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(marketData);
        
        var messageId = await db.StreamAddAsync(
            "marketdata:stream",
            "data",
            json,
            maxLength: 10000, // Prevent unbounded growth
            useApproximateMaxLength: true);

        return Results.Ok(new { messageId = messageId.ToString() });
    }
    catch (RedisConnectionException ex)
    {
        logger.LogError(ex, "Redis connection failed");
        return Results.StatusCode(503); // Service Unavailable
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error processing market data");
        return Results.StatusCode(500);
    }
});

static List<string> ValidateMarketData(MarketDataEvent data)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(data.Symbol))
        errors.Add("Symbol is required");
    
    if (data.Price <= 0)
        errors.Add("Price must be positive");
    
    if (data.Volume < 0)
        errors.Add("Volume cannot be negative");
    
    if (data.Timestamp > DateTime.UtcNow.AddMinutes(5))
        errors.Add("Timestamp cannot be in the future");
    
    return errors;
}
```

### 3. Broker Service Error Recovery
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await ConsumeMessagesAsync(stoppingToken);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection lost, reconnecting...");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in consumer, restarting...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

private async Task ConsumeMessagesAsync(CancellationToken ct)
{
    var db = _redis.GetDatabase();
    await EnsureConsumerGroupExists(db);
    
    while (!ct.IsCancellationRequested)
    {
        var result = await db.StreamReadGroupAsync(
            StreamKey,
            ConsumerGroup,
            ConsumerName,
            ">",
            count: 10,
            noAck: false);

        foreach (var entry in result)
        {
            await ProcessMessageSafely(db, entry, ct);
        }
    }
}

private async Task ProcessMessageSafely(
    IDatabase db,
    StreamEntry entry,
    CancellationToken ct)
{
    try
    {
        await ProcessMessage(db, entry, ct);
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid JSON in message {MessageId}", entry.Id);
        // Acknowledge to prevent reprocessing
        await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing message {MessageId}", entry.Id);
        // Leave unacknowledged for retry
    }
}
```

### 4. React UI Error Handling
```typescript
export function useMarketData(brokerUrl: string): UseMarketDataResult {
  const [error, setError] = useState<string | null>(null);
  const [reconnectAttempts, setReconnectAttempts] = useState(0);
  const maxReconnectAttempts = 5;

  useEffect(() => {
    if (reconnectAttempts >= maxReconnectAttempts) {
      setError('Unable to connect after multiple attempts. Please refresh the page.');
      return;
    }

    const streamUrl = `${brokerUrl}/api/marketdata/stream`;
    const eventSource = new EventSource(streamUrl);

    eventSource.onerror = (e) => {
      console.error('SSE error:', e);
      setIsConnected(false);
      
      if (eventSource.readyState === EventSource.CLOSED) {
        setReconnectAttempts(prev => prev + 1);
        setError(`Connection lost. Reconnecting (attempt ${reconnectAttempts + 1}/${maxReconnectAttempts})...`);
      }
    };

    return () => eventSource.close();
  }, [brokerUrl, reconnectAttempts]);

  return { securities, isConnected, error };
}
```

## Verification

### 1. Network Failure Test
1. Start all services
2. Disconnect network
3. Verify graceful error messages
4. Reconnect network
5. Verify automatic recovery

### 2. Service Crash Test
1. Kill Redis container
2. Verify services log errors but don't crash
3. Restart Redis
4. Verify services reconnect automatically

### 3. Invalid Data Test
```bash
# Send invalid data
curl -X POST http://localhost:5001/api/marketdata \
  -H "Content-Type: application/json" \
  -d '{"symbol":"","price":-100}'
```
Expected: 400 Bad Request with validation errors

### 4. Load Test
1. Increase tick rate to 50ms
2. Run for 10 minutes
3. Verify no memory leaks
4. Check error logs for patterns

## Related Sections
- Technical Approach Section 7 (Phase 5: Polish)
- Technical Approach Section 8 (Testing)

## Estimated Effort
3-4 hours