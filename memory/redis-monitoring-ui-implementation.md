# Redis Stream Monitoring UI Feature - Implementation Plan

**Date:** January 24, 2026  
**Purpose:** Add UI feature for validating Redis Stream activity

---

## Overview

Create a dedicated page in the React application that displays Redis Streams statistics, helping users verify that the Market Simulator is publishing data correctly.

---

## Requirements Summary

### Functional Requirements
1. ✅ Display summary table of all Redis streams
2. ✅ Show for each stream:
   - Stream name
   - Total number of messages
   - Latest message timestamp
3. ✅ Refresh button to manually reload data
4. ✅ Fetch real-time data from Redis (not cached)
5. ✅ Informational section explaining Redis Streams

### UI Requirements
- Simple, readable design
- Tabular data format
- Educational content for beginners
- Acts as debugging aid and learning reference

---

## Implementation Checklist

### Phase 1: Backend API Development

#### 1.1 Create Redis Stream Info Endpoint (Broker.Service)
- [ ] Create `RedisStreamInfo` DTO in Shared.Contracts
  - StreamName (string)
  - MessageCount (long)
  - LatestMessageTimestamp (DateTime?)
  - ConsumerGroups (List<ConsumerGroupInfo>)
  - FirstEntryId (string?)
  - LastEntryId (string?)

- [ ] Create `ConsumerGroupInfo` DTO
  - GroupName (string)
  - ConsumersCount (int)
  - PendingCount (long)
  - LastDeliveredId (string?)

- [ ] Create GET endpoint `/api/redis/streams/info` in Broker.Service
  - Query Redis for stream information using XINFO STREAM
  - Query consumer group information using XINFO GROUPS
  - Return list of RedisStreamInfo objects
  - Handle errors gracefully (stream doesn't exist, Redis unavailable)

- [ ] Add proper error handling and logging

#### 1.2 Alternative: Create Redis Monitoring Controller
- [ ] Create `RedisMonitoringController.cs` in Broker.Service/Controllers
- [ ] Inject `IConnectionMultiplexer` 
- [ ] Implement `GetStreamsInfo()` method
- [ ] Add CORS support for frontend

### Phase 2: Frontend Development

#### 2.1 Create Types/Interfaces
- [ ] Add `RedisStreamInfo` interface in `src/types/index.ts`
- [ ] Add `ConsumerGroupInfo` interface

#### 2.2 Create API Service
- [ ] Create `src/services/redisMonitoringService.ts`
- [ ] Implement `fetchStreamsInfo()` function
- [ ] Handle fetch errors and timeouts

#### 2.3 Create React Components
- [ ] Create `src/pages/RedisMonitoring.tsx` (main page)
  - Use Material-UI components
  - Display streams in table format
  - Show loading state
  - Show error state
  - Include Refresh button
  
- [ ] Create `src/components/RedisStreamTable.tsx`
  - Table with columns: Stream Name, Messages, Latest Timestamp
  - Expandable rows to show consumer group details
  - Format timestamps nicely
  
- [ ] Create `src/components/RedisExplanation.tsx`
  - Informational section about Redis Streams
  - Explain message appending
  - Explain consumer groups
  - Explain how this app uses streams

#### 2.4 Add Routing
- [ ] Install `react-router-dom` if not already installed
- [ ] Update `src/App.tsx` to include routing
- [ ] Add `/redis-monitoring` route
- [ ] Add navigation to Redis Monitoring page

#### 2.5 Add Navigation Component
- [ ] Create `src/components/Navigation.tsx` or update existing
- [ ] Add tabs/links: "Dashboard" and "Redis Monitoring"

### Phase 3: Styling and Polish

- [ ] Apply consistent Material-UI theming
- [ ] Add responsive design for mobile
- [ ] Add loading spinner during refresh
- [ ] Add success/error notifications
- [ ] Format timestamps in user-friendly format
- [ ] Add tooltips for technical terms

### Phase 4: Testing

- [ ] Test endpoint with Redis running and streams populated
- [ ] Test endpoint when Redis is unavailable
- [ ] Test endpoint when no streams exist
- [ ] Test UI refresh functionality
- [ ] Test UI error handling
- [ ] Test navigation between pages
- [ ] Test responsive design

### Phase 5: Documentation

- [ ] Update README with new feature description
- [ ] Add screenshots to documentation
- [ ] Document API endpoint in architecture docs
- [ ] Add usage instructions

---

## Technical Approach

### Backend Implementation Details

**Redis Commands to Use:**
```bash
# Get stream info
XINFO STREAM marketdata:stream

# Get consumer groups
XINFO GROUPS marketdata:stream

# Get stream length
XLEN marketdata:stream

# Get latest message
XREVRANGE marketdata:stream + - COUNT 1
```

**C# Implementation Pattern:**
```csharp
// Get stream info
var db = redis.GetDatabase();
var streamInfo = await db.ExecuteAsync("XINFO", "STREAM", "marketdata:stream");
var length = await db.StreamLengthAsync("marketdata:stream");
```

### Frontend Implementation Details

**API Call Pattern:**
```typescript
const fetchStreamsInfo = async (): Promise<RedisStreamInfo[]> => {
  const response = await fetch(`${brokerUrl}/api/redis/streams/info`);
  if (!response.ok) throw new Error('Failed to fetch stream info');
  return response.json();
};
```

**Component Structure:**
```
src/
  pages/
    RedisMonitoring.tsx       (main page)
  components/
    RedisStreamTable.tsx      (table component)
    RedisExplanation.tsx      (info section)
    Navigation.tsx            (navigation)
  services/
    redisMonitoringService.ts (API calls)
```

---

## Educational Content to Include

### Redis Streams Explanation (for UI)

**What are Redis Streams?**
- Append-only log data structure in Redis
- Similar to Apache Kafka topics
- Designed for real-time data streaming

**How Messages are Appended:**
- Each message has a unique ID (timestamp-sequence)
- Messages are immutable once added
- Ordered by insertion time
- Can be trimmed to limit memory usage

**Consumer Groups:**
- Allow multiple consumers to read from same stream
- Each consumer tracks its own position
- Messages can be acknowledged (XACK)
- Supports horizontal scaling

**How This Application Uses Streams:**
- Market Simulator → publishes to `marketdata:stream`
- Broker Service → consumes from stream using consumer group `brokers`
- Each message contains: symbol, price, timestamp
- Stream is trimmed to last 10,000 messages

---

## Files to Create/Modify

### Backend
1. `src/Shared.Contracts/RedisStreamInfo.cs` (new)
2. `src/Shared.Contracts/ConsumerGroupInfo.cs` (new)
3. `src/Broker.Service/Controllers/RedisMonitoringController.cs` (new)

### Frontend
1. `src/frontend/src/types/index.ts` (modify)
2. `src/frontend/src/services/redisMonitoringService.ts` (new)
3. `src/frontend/src/pages/RedisMonitoring.tsx` (new)
4. `src/frontend/src/components/RedisStreamTable.tsx` (new)
5. `src/frontend/src/components/RedisExplanation.tsx` (new)
6. `src/frontend/src/components/Navigation.tsx` (new)
7. `src/frontend/src/App.tsx` (modify for routing)
8. `src/frontend/package.json` (if react-router-dom needs to be added)

---

## Success Criteria

- ✅ Users can view all Redis streams and their statistics
- ✅ Refresh button fetches latest data from Redis
- ✅ UI clearly shows when simulator is publishing data
- ✅ Error states are handled gracefully
- ✅ Educational content helps users understand Redis Streams
- ✅ Page is responsive and easy to use
- ✅ No performance impact on main trading dashboard

---

## Notes

- Keep the endpoint lightweight (read-only operations)
- Cache is acceptable for 1-2 seconds to avoid hammering Redis
- Consider adding auto-refresh option (every 5 seconds)
- Ensure proper error boundaries in React components
- Use Material-UI consistently with existing design

---

## Next Steps

1. Start with backend implementation (Phase 1)
2. Test endpoint manually with curl/Postman
3. Proceed to frontend implementation (Phase 2)
4. Integrate and test end-to-end
5. Polish and document
