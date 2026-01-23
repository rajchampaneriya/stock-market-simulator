import type { MarketDataEvent } from '../types';

export interface OHLC {
  timestamp: Date;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export class PriceAggregator {
  private tickWindow: MarketDataEvent[] = [];
  private windowSize: number;
  private ohlcHistory: OHLC[] = [];
  private maxHistory: number;

  constructor(windowSizeMs: number = 60000, maxHistory: number = 30) {
    this.windowSize = windowSizeMs;
    this.maxHistory = maxHistory;
  }

  addTick(tick: MarketDataEvent): OHLC | null {
    this.tickWindow.push(tick);
    
    const now = new Date(tick.timestamp).getTime();
    const oldestAllowed = now - this.windowSize;
    
    this.tickWindow = this.tickWindow.filter(
      t => new Date(t.timestamp).getTime() > oldestAllowed
    );

    if (this.tickWindow.length < 2) return null;

    const ohlc: OHLC = {
      timestamp: new Date(tick.timestamp),
      open: this.tickWindow[0].price,
      high: Math.max(...this.tickWindow.map(t => t.price)),
      low: Math.min(...this.tickWindow.map(t => t.price)),
      close: tick.price,
      volume: this.tickWindow.reduce((sum, t) => sum + t.volume, 0),
    };

    this.ohlcHistory.push(ohlc);
    
    if (this.ohlcHistory.length > this.maxHistory) {
      this.ohlcHistory.shift();
    }

    return ohlc;
  }

  getHistory(): OHLC[] {
    return [...this.ohlcHistory];
  }

  clear(): void {
    this.tickWindow = [];
    this.ohlcHistory = [];
  }
}
