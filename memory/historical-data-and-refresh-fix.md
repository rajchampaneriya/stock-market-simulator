# Historical Data & Page Refresh Fix

## Problem Statement
1. **Chart lacks density**: No historical candlestick data on load — system only streams live data via SSE. On connect, chart starts empty.
2. **Page fails on refresh**: React state resets, SSE reconnects, but no historical candles available — chart shows "Accumulating price data..."

## Root Cause Analysis
- `CandlestickAggregator` builds 1-minute OHLCV candles in memory but never persists them
- `SseController.Stream()` only sends new live events from the channel
- On client connect/reconnect, there's zero historical data available
- `PriceAggregator` (frontend) starts with an empty `completedCandles` array on every page load

## Solution Architecture
Send pre-generated historical candles via SSE on client connect, before live streaming begins.

### Backend Changes (Broker.Service)
- [x] 1. **Create `CandlestickHistory.cs`** (`Services/`) — In-memory per-symbol ring buffer for completed candles + seed historical data generator using RealisticPriceEngine logic
- [x] 2. **Modify `CandlestickAggregator.cs`** — Push completed candles into `CandlestickHistory`
- [x] 3. **Modify `SseController.cs`** — On new SSE connect, send all historical candles as `candlestick` events (isComplete=true) before entering live loop
- [x] 4. **Register `CandlestickHistory` in `Program.cs`** as singleton

### Frontend Changes
- [x] 5. **Modify `PriceAggregator.ts`** — Add `loadHistoricalBatch()` method that ingests a sorted array of completed candles without duplicates
- [x] 6. **Modify `useMarketData.ts`** — Handle `history` SSE event type for batch delivery, or simply process historical `candlestick` events (existing logic handles this)

### Refresh Fix
- [x] 7. Historical candles on SSE connect fix the empty chart on refresh automatically

## Technical Details
- Seed ~30 historical 1-minute candles per symbol on startup
- Use same GBM + mean-reversion model as `RealisticPriceEngine`
- Store max 60 completed candles per symbol (rolling buffer)
- Historical candles sent before live loop so chart populates instantly

## Status: COMPLETE
