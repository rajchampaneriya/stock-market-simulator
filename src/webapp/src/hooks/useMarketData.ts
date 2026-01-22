import { useState, useEffect, useCallback, useRef } from 'react';
import type { MarketDataEvent, Security } from '../types';
import { PriceAggregator, type OHLC } from '../utils/priceAggregator';

interface UseMarketDataResult {
  securities: Map<string, Security>;
  isConnected: boolean;
  error: string | null;
  priceHistory: Map<string, OHLC[]>;
}

export function useMarketData(brokerUrl: string): UseMarketDataResult {
  const [securities, setSecurities] = useState<Map<string, Security>>(new Map());
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [priceHistory, setPriceHistory] = useState<Map<string, OHLC[]>>(new Map());
  const aggregators = useRef<Map<string, PriceAggregator>>(new Map());

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

    if (!aggregators.current.has(event.symbol)) {
      aggregators.current.set(event.symbol, new PriceAggregator());
    }

    const aggregator = aggregators.current.get(event.symbol)!;
    const ohlc = aggregator.addTick(event);

    if (ohlc) {
      setPriceHistory((prev) => {
        const updated = new Map(prev);
        updated.set(event.symbol, aggregator.getHistory());
        return updated;
      });
    }
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

    return () => {
      console.log('Closing SSE connection');
      eventSource.close();
      setIsConnected(false);
    };
  }, [brokerUrl, updateSecurity]);

  return { securities, isConnected, error, priceHistory };
}
