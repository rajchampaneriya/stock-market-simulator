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
