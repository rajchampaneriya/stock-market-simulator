# Realistic Market Data & 1-Minute Candlestick Aggregation — Implementation Summary

**Date:** 2026-02-07  
**Branch:** sse-implementation  
**Status:** ✅ Complete — all builds pass, all 53 tests pass

---

## What Changed

### Problem
The candlestick chart showed unrealistic patterns because:
1. Price generation used pure random noise (`Random.NextDouble() - 0.5`), producing no recognizable market structure
2. The frontend `PriceAggregator` created a new OHLC entry for every tick instead of grouping ticks into fixed 1-minute candles
3. There was no backend aggregation pipeline — all data processing was on the client

### Solution Architecture

```
ExchangeSimulator (RealisticPriceEngine — GBM + GARCH + mean reversion)
  → MarketIngestion.Api → Redis Stream
    → Broker.Service / RedisStreamConsumer
        ↓ TickBroadcaster (fan-out)
        ├── Channel<MarketDataEvent> → SSE "marketdata" events → Watchlist, Banner
        └── AggregatorTickChannel → CandlestickAggregator
                                    → Channel<CandlestickEvent> → SSE "candlestick" events → Chart
```

---

## Files Modified / Created

### Created
| File | Purpose |
|------|---------|
| `src/MarketSimulator/RealisticPriceEngine.cs` | GBM + GARCH volatility clustering + Ornstein-Uhlenbeck mean reversion + micro-trends |
| `src/Shared.Contracts/CandlestickEvent.cs` | OHLCV candle contract with `IsComplete` flag |
| `src/Broker.Service/Services/CandlestickAggregator.cs` | Backend 1-minute OHLCV aggregation pipeline |
| `src/Broker.Service/Services/TickBroadcaster.cs` | Fan-out broadcaster + `AggregatorTickChannel` wrapper |

### Modified
| File | Changes |
|------|---------|
| `src/MarketSimulator/ExchangeSimulator.cs` | Replaced raw random with `RealisticPriceEngine`, per-symbol profiles |
| `src/Broker.Service/RedisStreamConsumer.cs` | Writes via `TickBroadcaster.Broadcast()` instead of single channel |
| `src/Broker.Service/Program.cs` | Registered `CandlestickAggregator`, `TickBroadcaster`, `AggregatorTickChannel`, `Channel<CandlestickEvent>` |
| `src/Broker.Service/Controllers/SseController.cs` | Multiplexes tick + candle channels into single SSE stream; new `candlestick` event type |
| `src/frontend/src/types/index.ts` | Added `CandlestickEvent` interface |
| `src/frontend/src/utils/priceAggregator.ts` | Rewritten to consume backend candle events; maintains completed + in-progress candles |
| `src/frontend/src/hooks/useMarketData.ts` | Listens for `candlestick` SSE events; ticks still drive watchlist/banner |

---

## Key Design Decisions

### 1. Realistic Price Engine
- **Geometric Brownian Motion (GBM)** — multiplicative log-returns prevent negative prices
- **GARCH(1,1) volatility clustering** — high-volatility periods cluster together, creating realistic chart "bursts"
- **Ornstein-Uhlenbeck mean reversion** — prices drift back toward `BasePrice`, preventing runaway walks
- **Micro-trends** — periodic drift shifts create recognizable trend segments (40-200 ticks)
- **Per-symbol profiles** — NIFTY50 (14% vol), BANKNIFTY (18%), RELIANCE (20%), TCS (16%)

### 2. Backend Aggregation Pipeline
- `CandlestickAggregator` runs as a `BackgroundService` consuming from its own dedicated channel
- Maintains one in-progress candle per symbol, emitting throttled updates every 500ms
- On minute boundary → emits `IsComplete = true`, starts new candle
- Periodic timer (1s) ensures candle finalization even when ticks are sparse

### 3. Fan-out Broadcasting
- `TickBroadcaster` writes each tick to both the SSE channel and the aggregator channel
- `AggregatorTickChannel` is a typed wrapper to avoid DI ambiguity with `Channel<MarketDataEvent>`
- Both channels use `BoundedChannelFullMode.DropOldest` for backpressure safety

### 4. SSE Multiplexing
- `SseController.StreamEventsAsync` multiplexes tick and candle channels via `Task.WhenAny` + `TryRead`
- Two event types in one SSE connection: `"marketdata"` (ticks) and `"candlestick"` (OHLCV)
- Frontend routes ticks → watchlist/banner, candles → chart

### 5. Frontend Candle Management
- `PriceAggregator` now simply stores completed candles + one in-progress candle
- In-progress candle updates in real-time (OHLCV changes within same minute)
- Completed candles are immutable in history (up to 60 candles = 1 hour view)

---

## Verification
- ✅ `dotnet build` — 0 warnings, 0 errors
- ✅ `npx tsc --noEmit` — frontend type-checks clean
- ✅ 53/53 tests pass (Broker.Service: 1, MarketIngestion.Api: 19, MarketSimulator: 19, Shared.Contracts: 14)
