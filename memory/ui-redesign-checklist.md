# UI Redesign Checklist — TradingView-Inspired Professional Dashboard

## Goal
Transform the Stock Market Simulator frontend from a basic MUI card layout into a polished, TradingView-inspired professional trading dashboard that showcases strong architectural and engineering capabilities.

## Design Principles
- **Dark theme** with TradingView's signature color palette (#131722 background, #1e222d panels)
- **Professional typography** using Inter/system fonts with clear hierarchy
- **Information density** — show more data in less space, like a real trading terminal
- **Color coding** — Green (#26a69a) for bullish, Red (#ef5350) for bearish
- **Responsive grid layout** — Full-width dashboard feel, not boxed cards
- **Real-time feel** — Pulsing indicators, subtle animations, connection status

---

## Checklist

### 1. MUI Theme Configuration ✅
- [x] Create custom dark theme (`src/theme.ts`)
- [x] TradingView palette: bg #131722, surface #1e222d, paper #2a2e39
- [x] Green (#26a69a) / Red (#ef5350) for price movements
- [x] Inter font family with proper weight hierarchy
- [x] Custom component overrides for Card, AppBar, Chip, etc.

### 2. Global Styles & HTML ✅
- [x] Update `index.html` — title "Stock Market Simulator | Real-Time Trading Dashboard"
- [x] Rewrite `index.css` — dark background, scrollbar styling, font imports
- [x] Rewrite `App.css` — animations (pulse, glow, fade-in, price flash)

### 3. Navigation Redesign ✅
- [x] Slim, dark header bar with logo area and live connection indicator
- [x] Pill-style active tab indicator
- [x] Clock/timestamp display
- [x] Architecture badge ("Aspire • Redis Streams • SSE")

### 4. Dashboard Layout Redesign ✅
- [x] Full-width layout (no Container maxWidth restriction)
- [x] Top ticker strip with horizontally scrolling prices
- [x] Main area: selected stock's large candlestick chart
- [x] Side panel: watchlist with compact stock rows
- [x] Bottom status bar with connection info and metrics

### 5. Stock Ticker / Watchlist ✅
- [x] Compact rows instead of large cards
- [x] Inline sparkline or mini price indicator
- [x] Click to select → shows full chart in main panel
- [x] Color-coded price changes with flash animation
- [x] Volume bars or indicators

### 6. Professional Candlestick Chart ✅
- [x] Full-width chart taking main viewport area
- [x] Dark background matching theme
- [x] Crosshair cursor with price/time tooltip
- [x] Volume bars below price chart
- [x] Grid lines with subtle opacity
- [x] Price scale on right, time scale on bottom
- [x] Chart header with symbol, price, and change info

### 7. Redis Monitoring Page ✅
- [x] Dark-themed tables matching overall design
- [x] Terminal/console-style aesthetic for stream data
- [x] Consistent styling with dashboard

### 8. Final Polish ✅
- [x] Update page title and favicon concept
- [x] Verify responsive behavior
- [x] Build without errors

---

## Files to Modify
| File | Action |
|------|--------|
| `index.html` | Update title, add font link |
| `src/main.tsx` | Wrap with ThemeProvider |
| `src/theme.ts` | NEW — MUI theme config |
| `src/index.css` | Rewrite — dark global styles |
| `src/App.css` | Rewrite — animations |
| `src/App.tsx` | Add ThemeProvider, CssBaseline |
| `src/components/Navigation.tsx` | Redesign — slim dark header |
| `src/pages/Dashboard.tsx` | Redesign — full trading layout |
| `src/components/StockCard.tsx` | Replace with WatchlistItem |
| `src/components/CandlestickChart.tsx` | Pro chart rendering |
| `src/pages/RedisMonitoring.tsx` | Dark theme alignment |
| `src/components/RedisStreamTable.tsx` | Dark theme alignment |
| `src/components/RedisExplanation.tsx` | Dark theme alignment |

## Status: COMPLETE
