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
