import type { CandlestickEvent } from '../types';

/**
 * OHLCV data structure used by the CandlestickChart component.
 */
export interface OHLC {
  timestamp: Date;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

/**
 * Manages a rolling window of 1-minute candles received from the backend
 * CandlestickAggregator.
 *
 * The backend sends two kinds of candlestick events:
 *  - In-progress (isComplete === false): updates the current (latest) candle
 *  - Completed   (isComplete === true):  finalises a candle and locks it in history
 *
 * This class simply maps those events into the OHLC[] array consumed by the chart.
 */
export class PriceAggregator {
  private completedCandles: OHLC[] = [];
  private currentCandle: OHLC | null = null;
  private currentIntervalKey: string | null = null;
  private readonly maxHistory: number;

  constructor(_windowSizeMs: number = 60000, maxHistory: number = 60) {
    this.maxHistory = maxHistory;
  }

  /**
   * Processes a backend CandlestickEvent. Returns the full OHLC history
   * (completed candles + the in-progress candle) or null if nothing to show yet.
   */
  addCandlestickEvent(event: CandlestickEvent): OHLC[] | null {
    const intervalKey = event.intervalStart;

    if (event.isComplete) {
      // Finalise this candle
      const candle: OHLC = {
        timestamp: new Date(event.intervalStart),
        open: event.open,
        high: event.high,
        low: event.low,
        close: event.close,
        volume: event.volume,
      };

      // Only add if we don't already have this interval
      const lastCompleted = this.completedCandles[this.completedCandles.length - 1];
      if (!lastCompleted || lastCompleted.timestamp.getTime() !== candle.timestamp.getTime()) {
        this.completedCandles.push(candle);
      } else {
        // Update in place (e.g., late completion emit)
        this.completedCandles[this.completedCandles.length - 1] = candle;
      }

      // Trim history
      if (this.completedCandles.length > this.maxHistory) {
        this.completedCandles.shift();
      }

      // Clear current candle — next in-progress event will start a new one
      this.currentCandle = null;
      this.currentIntervalKey = null;
    } else {
      // In-progress update — update (or create) the current candle
      if (this.currentIntervalKey !== intervalKey) {
        // New interval started
        this.currentIntervalKey = intervalKey;
      }
      this.currentCandle = {
        timestamp: new Date(event.intervalStart),
        open: event.open,
        high: event.high,
        low: event.low,
        close: event.close,
        volume: event.volume,
      };
    }

    return this.getHistory();
  }

  /**
   * Legacy method — kept for backward compatibility but now a no-op.
   * Tick data is used only by watchlist/banner, not by the chart.
   */
  addTick(): OHLC | null {
    return null;
  }

  getHistory(): OHLC[] {
    const result = [...this.completedCandles];
    if (this.currentCandle) {
      result.push(this.currentCandle);
    }
    return result;
  }

  clear(): void {
    this.completedCandles = [];
    this.currentCandle = null;
    this.currentIntervalKey = null;
  }
}
