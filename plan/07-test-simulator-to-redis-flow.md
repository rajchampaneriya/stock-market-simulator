# Task 07: Test Simulator → Ingestion → Redis Flow

## Objective
Verify the complete flow from market data generation through ingestion to Redis storage.

## Prerequisites
- Task 05 completed (Market Simulator working)
- Task 06 completed (Ingestion API working)
- Task 03 completed (Redis running)

## Acceptance Criteria
- [ ] Simulator successfully sends data to Ingestion API
- [ ] Ingestion API successfully publishes to Redis Streams
- [ ] No errors in logs for both services
- [ ] Redis stream contains market data messages
- [ ] Messages have correct structure and data
- [ ] Stream length increases over time
- [ ] Performance is acceptable (< 300ms latency)

## Test Steps

### 1. Start Services
```bash
# Terminal 1: Start Redis (via Aspire or standalone)
docker run -p 6379:6379 redis:7

# Terminal 2: Start Ingestion API
dotnet run --project src/MarketIngestion.Api

# Terminal 3: Start Market Simulator
dotnet run --project src/MarketSimulator
```

### 2. Monitor Redis Stream
```bash
# Check stream length (should increase)
watch -n 1 'redis-cli XLEN marketdata:stream'

# View recent messages
redis-cli XRANGE marketdata:stream - + COUNT 10

# Monitor in real-time
redis-cli XREAD COUNT 5 STREAMS marketdata:stream 0
```

### 3. Check Service Logs
- Simulator logs: Should show successful POST requests
- Ingestion logs: Should show message publishing
- No errors or exceptions

### 4. Verify Data Quality
```bash
# Get latest message
redis-cli XREVRANGE marketdata:stream + - COUNT 1
```

Expected JSON structure:
```json
{
  "symbol": "NIFTY50",
  "price": 1000.23,
  "volume": 5432,
  "timestamp": "2026-01-22T01:30:00.000Z"
}
```

## Performance Check
- Use Aspire dashboard to view traces
- Verify end-to-end latency < 300ms
- Check CPU and memory usage
- Ensure no memory leaks over 5 minutes

## Troubleshooting
- **Connection refused**: Check Ingestion API is running on port 5001
- **Redis errors**: Verify Redis is accessible on localhost:6379
- **No data in stream**: Check simulator logs for errors
- **High latency**: Review tick interval configuration

## Related Sections
- Technical Approach Section 3.2 (Data Flow Sequence)
- Technical Approach Section 7 (Phase 2: Core Services)
- Technical Approach Section 8.3 (Redis Debugging)

## Estimated Effort
1-2 hours
