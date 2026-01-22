# Task 11: Implement SSE Client Hook (useMarketData)

## Objective
Create a React hook that connects to the SSE endpoint and manages market data state.

## Prerequisites
- Task 10 completed (React app setup)
- Broker Service SSE endpoint working

## Acceptance Criteria
- [ ] `useMarketData.ts` hook created
- [ ] EventSource API properly integrated
- [ ] State management for securities map
- [ ] Connection status tracking
- [ ] Auto-reconnection on disconnect
- [ ] Event handlers for all event types (connected, marketdata, heartbeat)
- [ ] Cleanup on unmount
- [ ] TypeScript types properly defined
- [ ] Error handling implemented

## Implementation

### src/WebApp/src/hooks/useMarketData.ts
```typescript
import { useState, useEffect, useCallback } from 'react';
import { MarketDataEvent, Security } from '../types';

interface UseMarketDataResult {
  securities: Map<string, Security>;
  isConnected: boolean;
  error: string | null;
}

export function useMarketData(brokerUrl: string): UseMarketDataResult {
  const [securities, setSecurities] = useState<Map<string, Security>>(new Map());
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const updateSecurity = useCallback((event: MarketDataEvent) => {
    setSecurities((prev) => {
      const updated = new Map(prev);
      const existing = updated.get(event.symbol);

      const security: Security = {
        symbol: event.symbol,
        lastPrice: event.price,
        previousPrice: existing?.lastPrice ?? event.price,
        volume: event.volume,
        lastUpdate: event.timestamp,
        change: 0,
        changePercent: 0,
      };

      security.change = security.lastPrice - security.previousPrice;
      security.changePercent = security.previousPrice > 0
        ? (security.change / security.previousPrice) * 100
        : 0;

      updated.set(event.symbol, security);
      return updated;
    });
  }, []);

  useEffect(() => {
    const streamUrl = `${brokerUrl}/api/marketdata/stream`;
    console.log('Connecting to SSE:', streamUrl);

    const eventSource = new EventSource(streamUrl);

    eventSource.addEventListener('connected', (e) => {
      const data = JSON.parse(e.data);
      console.log('Connected to broker:', data.clientId);
      setIsConnected(true);
      setError(null);
    });

    eventSource.addEventListener('marketdata', (e) => {
      const marketData: MarketDataEvent = JSON.parse(e.data);
      updateSecurity(marketData);
    });

    eventSource.addEventListener('heartbeat', (e) => {
      const data = JSON.parse(e.data);
      console.debug('Heartbeat:', data.timestamp);
    });

    eventSource.onerror = (e) => {
      console.error('SSE error:', e);
      setIsConnected(false);
      setError('Connection lost. Reconnecting...');
    };

    eventSource.onopen = () => {
      console.log('SSE connection opened');
      setIsConnected(true);
      setError(null);
    };

    // Cleanup
    return () => {
      console.log('Closing SSE connection');
      eventSource.close();
      setIsConnected(false);
    };
  }, [brokerUrl, updateSecurity]);

  return { securities, isConnected, error };
}
```

### Update App.tsx to Use Hook
```typescript
import React from 'react';
import { Container, Typography, Box, Alert } from '@mui/material';
import { useMarketData } from './hooks/useMarketData';

function App() {
  const brokerUrl = process.env.VITE_BROKER_URL || 'http://localhost:5002';
  const { securities, isConnected, error } = useMarketData(brokerUrl);

  return (
    <Container maxWidth="lg">
      <Box sx={{ my: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom>
          Stock Market Simulator
        </Typography>
        
        {error && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        
        {isConnected && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Connected - Receiving live data
          </Alert>
        )}
        
        <Typography variant="body1">
          Tracking {securities.size} securities
        </Typography>
      </Box>
    </Container>
  );
}

export default App;
```

## Verification

### 1. Browser Console Test
1. Start all services (Simulator, Ingestion, Broker)
2. Start React app: `npm start`
3. Open browser console
4. Expected logs:
   - "Connecting to SSE: http://localhost:5002/api/marketdata/stream"
   - "Connected to broker: [clientId]"
   - "SSE connection opened"
   - Periodic heartbeat logs

### 2. Network Tab Test
1. Open browser DevTools → Network tab
2. Look for EventSource connection to `/api/marketdata/stream`
3. Verify connection stays open
4. Check data is streaming continuously

### 3. State Test
1. Add console.log in updateSecurity to see state updates
2. Verify securities map grows as new symbols arrive
3. Check prices update in real-time

### 4. Reconnection Test
1. Stop Broker Service
2. Verify error message appears
3. Restart Broker Service
4. Verify auto-reconnection happens within 3 seconds

## Troubleshooting
- **CORS errors**: Check Broker Service CORS configuration
- **Connection fails**: Verify Broker Service is running on port 5002
- **No data**: Check that Simulator and Ingestion are running
- **Stale data**: Check EventSource event listeners are attached correctly

## Related Sections
- Technical Approach Section 5.5 (Web UI)
- Technical Approach Section 7 (Phase 4: UI)

## Estimated Effort
2-3 hours
