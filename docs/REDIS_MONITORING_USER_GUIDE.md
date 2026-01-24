# Redis Stream Monitoring UI - User Guide

## Overview

The Redis Stream Monitoring feature provides a dedicated page in the React application to view real-time statistics about Redis Streams. This feature helps developers and operators:

- **Validate** that the Market Simulator is publishing data correctly
- **Debug** data flow issues between services
- **Learn** about Redis Streams concepts
- **Monitor** stream health and consumer group status

---

## Accessing the Feature

### Via Navigation Bar

1. Start the application using the .NET Aspire AppHost
2. Open the React frontend (URL provided by Aspire dashboard)
3. Click the **"Redis Monitoring"** button in the top navigation bar
4. The page will automatically load current stream statistics

### Direct URL

Navigate to `/redis-monitoring` in your browser.

---

## Understanding the Interface

### 1. Page Header

- **Title**: "Redis Stream Monitoring"
- **Subtitle**: "View real-time Redis Streams activity and verify data flow"
- **Refresh Button**: Click to manually reload data from Redis
- **Last Updated**: Shows when data was last fetched

### 2. Active Streams Table

A table displaying all Redis streams with the following columns:

| Column | Description | Visual Indicator |
|--------|-------------|------------------|
| **Expand Arrow** | Click to show/hide consumer group details | ▼ / ► icon |
| **Stream Name** | Redis stream key (e.g., `marketdata:stream`) | Monospace font |
| **Message Count** | Total messages currently in the stream | Green badge if > 0 |
| **Latest Message** | Timestamp of the most recent message | Formatted date/time |
| **Consumer Groups** | Number of consumer groups consuming from this stream | Outlined badge |

#### Example Display

```
Stream Name: marketdata:stream
Message Count: 2,708 (green badge)
Latest Message: 1/24/2026, 12:35:47 AM
Consumer Groups: 1 group(s)
```

### 3. Consumer Group Details (Expandable)

Click the arrow icon to expand and see consumer group information:

| Field | Description | Visual Indicator |
|-------|-------------|------------------|
| **Group Name** | Consumer group identifier (e.g., `brokers`) | Monospace font |
| **Consumers** | Number of active consumer instances | Number |
| **Pending Messages** | Unacknowledged messages waiting for processing | Green (0) / Yellow (>0) badge |
| **Last Delivered ID** | Redis message ID of the last delivered message | Monospace font, small text |

#### What Each Field Means

- **Consumers**: How many instances of the Broker Service are running and consuming from this group
- **Pending Messages**: 
  - **0 (green)**: All messages have been acknowledged ✅ Good!
  - **> 0 (yellow)**: Some messages are waiting to be acknowledged ⚠️ May indicate processing issues
- **Last Delivered ID**: The Redis message ID (format: `timestamp-sequence`)

### 4. Educational Section

Three accordion panels explaining Redis Streams:

#### "How Messages are Appended"
- Unique IDs (timestamp-sequence format)
- Immutability of messages
- Automatic ordering by insertion time
- Stream trimming to limit memory

#### "Consumer Groups"
- Multiple consumers can read from the same stream
- Each consumer tracks its own position
- Message acknowledgment (XACK)
- Horizontal scaling support

#### "How This Application Uses Streams"
- Stream name: `marketdata:stream`
- Publisher: Market Simulator → Market Ingestion API
- Consumer: Broker Service using consumer group `brokers`
- Message format: symbol, price, volume, timestamp
- Retention: Last 10,000 messages

### 5. Debug Information Panel

Located at the bottom of the page:

- **Broker URL**: The backend service URL being queried
- **API Endpoint**: The full endpoint path for stream info

---

## Common Use Cases

### 1. Verify Market Simulator is Working

**Problem**: "Is the Market Simulator publishing data?"

**Solution**:
1. Navigate to Redis Monitoring page
2. Check the **Message Count** column
3. Click **Refresh** button after a few seconds
4. If the count increases, simulator is working ✅

**Expected Behavior**:
- Message count should increase steadily (every 3 seconds by default)
- Latest Message timestamp should be recent

### 2. Check Consumer Processing

**Problem**: "Is the Broker Service consuming messages?"

**Solution**:
1. Expand the stream row to see consumer group details
2. Check **Consumers** count (should be ≥ 1)
3. Check **Pending Messages** (should be 0 or low)

**Expected Behavior**:
- Consumers: 1 or more (depending on how many Broker instances are running)
- Pending Messages: 0 (all messages processed immediately)

**Warning Signs**:
- Consumers: 0 → Broker Service might not be running
- Pending Messages: High number → Consumer is falling behind or has errors

### 3. Debug Data Flow Issues

**Problem**: "Data isn't showing on the Dashboard"

**Troubleshooting Steps**:

1. **Check if messages are being published**:
   - Go to Redis Monitoring
   - Verify Message Count > 0
   - If 0, check Market Simulator and Ingestion API

2. **Check if messages are being consumed**:
   - Expand consumer group details
   - Verify Consumers > 0
   - Verify Pending Messages is low

3. **Check timestamps**:
   - Is Latest Message timestamp recent?
   - If old, simulator might have stopped

### 4. Learning About Redis Streams

**Use Case**: "I'm new to Redis Streams"

**Recommended Steps**:
1. Read the "About Redis Streams" section
2. Expand each accordion to learn:
   - How messages work
   - What consumer groups do
   - How this app uses streams
3. Observe the live data in the table
4. Click Refresh to see real-time updates

---

## Interpreting the Data

### Healthy System Indicators ✅

- **Message Count**: Steadily increasing
- **Latest Message**: Timestamp within last few seconds
- **Consumers**: 1 or more
- **Pending Messages**: 0

### Warning Signs ⚠️

| Symptom | Possible Cause | Action |
|---------|----------------|--------|
| Message Count = 0 | Simulator not running or publishing failing | Check Simulator and Ingestion API logs |
| Message Count not increasing | Simulator stopped or crashed | Restart Market Simulator |
| Latest Message old (> 1 minute) | Simulator paused or slow | Check simulator performance |
| Consumers = 0 | Broker Service not running | Start Broker Service |
| Pending Messages high (> 100) | Consumer slow or error in processing | Check Broker Service logs |
| No consumer groups | Consumer group not created | Broker Service may not have started correctly |

---

## Refresh Behavior

### Manual Refresh
- Click the **Refresh** button in the top right
- Data is fetched directly from Redis (no caching)
- Loading spinner appears while fetching
- Last Updated timestamp updates on success

### No Auto-Refresh
- The page does NOT automatically refresh
- This prevents overwhelming Redis with queries
- Use Refresh button to get latest data on demand

### When to Refresh
- After starting the Market Simulator
- When debugging data flow issues
- To verify consumer processing is working
- To check if pending messages are being cleared

---

## Error Handling

### Connection Errors

**Error Message**: "Failed to fetch stream info: 503 Service Unavailable"

**Cause**: Unable to connect to Redis

**Solution**:
1. Verify Redis container is running
2. Check Redis connection string in configuration
3. Restart Redis if needed

### No Data Available

**Display**: "No Redis streams found. The Market Simulator may not have started publishing data yet."

**Cause**: Stream doesn't exist or has never received messages

**Solution**:
1. Start the Market Simulator
2. Wait a few seconds
3. Click Refresh

### Empty Response

**Error Message**: "Failed to fetch stream information"

**Cause**: Backend service error

**Solution**:
1. Check Broker Service logs
2. Verify Broker Service is running
3. Check API endpoint URL in Debug Information panel

---

## Technical Details

### API Endpoint

```
GET /api/redis/streams/info
```

**Response Format**:
```json
[
  {
    "streamName": "marketdata:stream",
    "messageCount": 2708,
    "latestMessageTimestamp": "2026-01-24T00:35:47.123Z",
    "firstEntryId": "1769173100467-0",
    "lastEntryId": "1769214947123-0",
    "consumerGroups": [
      {
        "groupName": "brokers",
        "consumersCount": 2,
        "pendingCount": 0,
        "lastDeliveredId": "1769214946828-0"
      }
    ]
  }
]
```

### Redis Commands Used by Backend

The backend controller uses these Redis commands:

- `XLEN marketdata:stream` - Get total message count
- `XRANGE marketdata:stream - + COUNT 1` - Get first entry
- `XRANGE marketdata:stream - + COUNT 1 REV` - Get last entry
- `XINFO GROUPS marketdata:stream` - Get consumer group info

### Message ID Format

Redis message IDs have the format: `timestamp-sequence`

Example: `1769214947123-0`
- `1769214947123` = Unix timestamp in milliseconds
- `0` = Sequence number (for messages with same timestamp)

The UI extracts the timestamp and displays it as a human-readable date.

---

## Best Practices

### For Development

1. **Keep the page open** while developing to monitor data flow
2. **Refresh frequently** when making changes to services
3. **Check pending messages** after deploying new code
4. **Monitor consumer count** to verify horizontal scaling

### For Production Monitoring

1. **Verify consumers > 0** before marking deployment as successful
2. **Alert on pending messages > threshold** (e.g., > 1000)
3. **Monitor message count trends** to detect simulator issues
4. **Check timestamps** to ensure data is fresh

### For Learning

1. **Read the educational section** before diving into code
2. **Experiment with stopping/starting services** and observe the changes
3. **Compare message IDs** to understand Redis's time-based IDs
4. **Watch consumer count** when scaling services up/down

---

## Troubleshooting Guide

### Problem: Page loads but shows "Loading..." forever

**Diagnosis**:
- Check browser console for JavaScript errors
- Verify broker URL is correct (check Debug Information panel)
- Test API endpoint manually with curl

**Solution**:
```bash
# Test endpoint (replace port with your broker port)
curl -k -s https://localhost:PORT/api/redis/streams/info
```

### Problem: "No Redis streams found" but simulator is running

**Diagnosis**:
- Check if Ingestion API is receiving requests
- Verify Redis connection in Ingestion API
- Check Market Simulator logs for errors

**Solution**:
1. Check Ingestion API health: `/health`
2. Verify simulator is sending HTTP requests
3. Check Redis directly:
   ```bash
   redis-cli XLEN marketdata:stream
   ```

### Problem: Consumer count is 0 but Broker Service is running

**Diagnosis**:
- Consumer group might not be created
- Broker Service might be using wrong stream name
- Redis connection issue in Broker Service

**Solution**:
1. Check Broker Service logs for connection errors
2. Verify stream name matches in all services
3. Check consumer group manually:
   ```bash
   redis-cli XINFO GROUPS marketdata:stream
   ```

---

## Navigation

- **← Dashboard**: Return to main stock market view
- **Redis Monitoring** (current page)

Use the navigation bar at the top to switch between pages.

---

## Summary

The Redis Stream Monitoring page is a powerful tool for:
- ✅ **Validating** that your data pipeline is working
- 🐛 **Debugging** issues with message flow
- 📚 **Learning** about Redis Streams architecture
- 📊 **Monitoring** system health in real-time

Keep this page open during development and use it as your go-to resource for understanding the health of your streaming data system.
