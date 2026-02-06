# Realistic Market Data & 1-Minute Candlestick Aggregation — Implementation Checklist

**Created:** 2026-02-07  
**Branch:** sse-implementation  
**Goal:** Replace random data generation with realistic price patterns, implement proper 1-minute candlestick aggregation on the backend, and update the frontend to display candles correctly.

---

## Problem Decomposition

### Current Issues
1. **Random Price Generation**: `ExchangeSimulator.GenerateTick()` uses `random.NextDouble() - 0.5` producing purely random walks with no recognizable market patterns (trends, mean reversion, volatility clustering)
2. **Frontend Aggregation Only**: The `PriceAggregator.ts` on the frontend creates a sliding window of ticks rather than proper fixed-interval candles — each tick produces a new OHLC entry, resulting in one candle per tick
3. **No Backend Aggregation**: The system lacks a server-side candlestick aggregation pipeline, meaning all data processing burden falls on the UI

### Target Architecture
```
ExchangeSimulator (realistic ticks)
  → MarketIngestion.Api (Redis Streams)
    → Broker.Service
      ├── RedisStreamConsumer → Channel<MarketDataEvent> → SSE (tick stream, unchanged)
      └── CandlestickAggregator (NEW) → Channel<CandlestickEvent> → SSE (candle stream)
        
Frontend:
  - Watchlist/Banner: consumes tick SSE (unchanged)
  - CandlestickChart: consumes candle SSE (new event type)
```

---

## Checklist

### Phase 1: Realistic Price Engine (Backend — MarketSimulator)
- [x] **1.1** Create `RealisticPriceEngine.cs` with configurable market behavior:
  - Geometric Brownian Motion (GBM) base model
  - Trend component (drift μ that shifts periodically)
  - Volatility clustering (GARCH-like: high-vol follows high-vol)
  - Mean reversion toward a base price
  - Occasional momentum bursts / micro-trends
  - Intraday volume patterns (higher at open/close)
- [x] **1.2** Update `ExchangeSimulator.cs` to use `RealisticPriceEngine` instead of raw random
- [x] **1.3** Configure per-symbol parameters (different volatility for NIFTY50 vs TCS, etc.)

### Phase 2: Backend Candlestick Aggregation Pipeline (Broker.Service)
- [x] **2.1** Create `CandlestickEvent` contract in `Shared.Contracts`
- [x] **2.2** Create `CandlestickAggregator.cs` in Broker.Service:
  - Maintains current 1-minute candle per symbol
  - On each tick: update OHLCV of current candle
  - On minute boundary: close candle, emit completed candle, start new one
  - Emit in-progress candle updates at a configurable throttle (e.g., every 500ms)
- [x] **2.3** Register `CandlestickAggregator` as a hosted service in Broker.Service DI
- [x] **2.4** Add `candlestick` SSE event type to `SseController.cs` that streams candle updates
- [x] **2.5** Add a Channel<CandlestickEvent> for candle data broadcast

### Phase 3: Frontend — Proper Candlestick Display
- [x] **3.1** Add `CandlestickEvent` type to `types/index.ts`
- [x] **3.2** Rewrite `PriceAggregator.ts` to process candle events from backend:
  - Maintain history of completed candles
  - Update the "current" (in-progress) candle in real-time
  - No longer generate OHLC from raw ticks
- [x] **3.3** Update `useMarketData.ts` hook:
  - Listen for new `candlestick` SSE event type
  - Pass candle data to chart, tick data to watchlist/banner (unchanged)
- [x] **3.4** Update `CandlestickChart.tsx` — no changes needed, already renders OHLC[] correctly
- [x] **3.5** Verify watchlist (`StockCard.tsx`) and banner still receive tick-by-tick data

### Phase 4: Verification & Documentation
- [x] **4.1** Build solution and verify no compile errors
- [x] **4.2** Save final implementation summary to `/memory`

---

## Implementation Status

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1 | ✅ Complete | RealisticPriceEngine with GBM + GARCH + mean reversion |
| Phase 2 | ✅ Complete | CandlestickAggregator + TickBroadcaster + Channel plumbing |
| Phase 3 | ✅ Complete | Frontend consumes candlestick SSE events |
| Phase 4 | ✅ Complete | All builds pass, 53/53 tests pass |
