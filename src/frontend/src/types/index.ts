export interface MarketDataEvent {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export interface Security {
  symbol: string;
  lastPrice: number;
  previousPrice: number;
  volume: number;
  lastUpdate: string;
  change: number;
  changePercent: number;
}

/**
 * Backend-produced candlestick event (1-minute OHLCV).
 * Streamed via SSE "candlestick" event type.
 */
export interface CandlestickEvent {
  symbol: string;
  intervalStart: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  tickCount: number;
  isComplete: boolean;
}

export interface ConsumerGroupInfo {
  groupName: string;
  consumersCount: number;
  pendingCount: number;
  lastDeliveredId: string | null;
}

export interface RedisStreamInfo {
  streamName: string;
  messageCount: number;
  latestMessageTimestamp: string | null;
  firstEntryId: string | null;
  lastEntryId: string | null;
  consumerGroups: ConsumerGroupInfo[];
}
