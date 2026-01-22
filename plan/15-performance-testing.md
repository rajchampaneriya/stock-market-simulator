# Task 15: Performance Testing and Optimization

## Objective
Conduct comprehensive performance testing and optimize the system for production-like conditions.

## Prerequisites
- All services implemented and integrated (Tasks 01-14 completed)

## Acceptance Criteria
- [ ] Latency measurements recorded (p50, p95, p99)
- [ ] Throughput tested with 10+ symbols
- [ ] CPU and memory usage profiled
- [ ] No memory leaks detected
- [ ] Redis stream backlog monitored
- [ ] OpenTelemetry traces analyzed
- [ ] Performance bottlenecks identified and resolved
- [ ] Target: < 300ms end-to-end latency (p95)
- [ ] System stable under load for 30+ minutes

## Test Scenarios

### 1. Baseline Performance
```bash
# Configuration
Symbols: 4 (NIFTY50, BANKNIFTY, RELIANCE, TCS)
Tick Interval: 300ms
Duration: 10 minutes
```

**Metrics to Collect:**
- Messages per second
- End-to-end latency (Simulator → Browser)
- Redis stream length
- CPU usage per service
- Memory usage per service
- Network bandwidth

### 2. High Load Test
```bash
# Configuration
Symbols: 20
Tick Interval: 100ms
Duration: 30 minutes
```

**Expected Results:**
- System remains stable
- No message loss
- Latency < 500ms (p95)
- Memory usage stable (no leaks)

### 3. Multiple Clients Test
- Open 10 browser tabs
- Each connected to SSE endpoint
- Verify all receive same data
- Check server resource usage

## Performance Monitoring

### Using Aspire Dashboard
1. Navigate to http://localhost:15888
2. View Traces tab
3. Filter by service and operation
4. Analyze latency distribution
5. Identify slow operations

### Using Redis CLI
```bash
# Monitor commands
redis-cli --latency-history

# Stream statistics
redis-cli XINFO STREAM marketdata:stream

# Consumer group lag
redis-cli XINFO GROUPS marketdata:stream
redis-cli XPENDING marketdata:stream brokers
```

### Using Browser DevTools
1. Open Performance tab
2. Record 30 seconds
3. Check for:
   - Long tasks (> 50ms)
   - Memory leaks
   - Excessive repaints
   - Network waterfall

## Optimization Techniques

### 1. Redis Stream Optimization
```csharp
// Batch processing
var result = await db.StreamReadGroupAsync(
    StreamKey,
    ConsumerGroup,
    ConsumerName,
    ">",
    count: 100); // Increase batch size

// Stream trimming
await db.StreamAddAsync(
    "marketdata:stream",
    "data",
    json,
    maxLength: 10000,
    useApproximateMaxLength: true);
```

### 2. SSE Connection Pooling
```csharp
// Use connection pooling
builder.Services.AddSingleton<ConnectionPool>();

// Reuse channels
var channel = Channel.CreateBounded<MarketDataEvent>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.DropOldest
});
```

### 3. React Optimization
```typescript
// Memoize expensive calculations
const StockCard = React.memo<StockCardProps>(({ security }) => {
  // Component implementation
}, (prev, next) => {
  return prev.security.lastUpdate === next.security.lastUpdate;
});

// Virtualize long lists
import { FixedSizeGrid } from 'react-window';
```

### 4. Reduce Payload Size
```csharp
// Use MessagePack instead of JSON (optional)
// Or compress JSON
var json = JsonSerializer.Serialize(marketData, new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});
```

## Verification

### 1. Latency Test
```bash
# Use curl to measure response times
for i in {1..100}; do
  curl -w "%{time_total}\n" -o /dev/null -s \
    -X POST http://localhost:5001/api/marketdata \
    -H "Content-Type: application/json" \
    -d '{"symbol":"TEST","price":100,"volume":1000,"timestamp":"2026-01-22T00:00:00Z"}'
done | awk '{sum+=$1; count++} END {print "Average:", sum/count}'
```

### 2. Memory Leak Test
```bash
# Monitor memory usage over time
dotnet-counters monitor --process-id $(pgrep -f "Broker.Service") \
  --counters System.Runtime[gc-heap-size,gen-0-gc-count,gen-1-gc-count]
```

### 3. Load Test with k6
```javascript
// load-test.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 10,
  duration: '30s',
};

export default function () {
  const payload = JSON.stringify({
    symbol: 'TEST',
    price: Math.random() * 1000,
    volume: Math.floor(Math.random() * 10000),
    timestamp: new Date().toISOString(),
  });

  const res = http.post('http://localhost:5001/api/marketdata', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 300ms': (r) => r.timings.duration < 300,
  });
}
```

Run: `k6 run load-test.js`

## Performance Targets

| Metric | Target | Acceptable | Poor |
|--------|--------|------------|------|
| Latency (p95) | < 300ms | < 500ms | > 500ms |
| Throughput | > 100 msg/s | > 50 msg/s | < 50 msg/s |
| CPU Usage | < 30% | < 50% | > 50% |
| Memory | Stable | < 500MB growth/hr | Constant growth |
| Message Loss | 0% | 0% | Any |

## Related Sections
- Technical Approach Section 7 (Phase 5: Polish)
- Technical Approach Section 8.2 (Performance Validation)

## Estimated Effort
4-6 hours
