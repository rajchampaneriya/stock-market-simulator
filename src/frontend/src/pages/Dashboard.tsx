import { useState, useMemo } from 'react';
import {
  Typography,
  Box,
  Alert,
  CircularProgress,
} from '@mui/material';
import { useMarketData } from '../hooks/useMarketData';
import { StockCard } from '../components/StockCard';
import { CandlestickChart } from '../components/CandlestickChart';
import type { Security } from '../types';

/**
 * TradingView-inspired trading dashboard
 */
export function Dashboard() {
  const brokerUrl = import.meta.env.DEV ? '' : (import.meta.env.VITE_BROKER_URL || 'http://localhost:5002');
  const { securities, isConnected, error, priceHistory } = useMarketData(brokerUrl);
  const [selectedSymbol, setSelectedSymbol] = useState<string | null>(null);

  const securitiesArray = useMemo(() => Array.from(securities.values()), [securities]);

  // Auto-select first security
  const activeSymbol = selectedSymbol && securities.has(selectedSymbol)
    ? selectedSymbol
    : securitiesArray[0]?.symbol ?? null;

  const activeSecurity: Security | undefined = activeSymbol
    ? securities.get(activeSymbol)
    : undefined;

  const activeHistory = activeSymbol ? priceHistory.get(activeSymbol) : undefined;

  return (
    <Box sx={{ display: 'flex', flex: 1, overflow: 'hidden', height: 'calc(100vh - 49px)' }}>
      {/* === Main Chart Area === */}
      <Box
        sx={{
          flex: 1,
          display: 'flex',
          flexDirection: 'column',
          minWidth: 0,
          borderRight: '1px solid',
          borderColor: 'divider',
        }}
      >
        {/* Ticker strip */}
        <Box
          className="ticker-strip"
          sx={{
            display: 'flex',
            gap: 0,
            px: 0,
            overflowX: 'auto',
            '&::-webkit-scrollbar': { height: 0 },
          }}
        >
          {securitiesArray.map((sec) => {
            const isPositive = sec.change > 0;
            const isNegative = sec.change < 0;
            return (
              <Box
                key={sec.symbol}
                onClick={() => setSelectedSymbol(sec.symbol)}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                  px: 2,
                  py: 0.5,
                  cursor: 'pointer',
                  borderRight: '1px solid #2a2e39',
                  bgcolor: sec.symbol === activeSymbol ? 'rgba(41, 98, 255, 0.08)' : 'transparent',
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.03)' },
                  transition: 'background-color 0.15s',
                  minWidth: 'fit-content',
                }}
              >
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: '#d1d4dc' }}>
                  {sec.symbol}
                </Typography>
                <Typography
                  sx={{
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    fontFamily: "'JetBrains Mono', monospace",
                    color: isPositive ? '#26a69a' : isNegative ? '#ef5350' : '#d1d4dc',
                  }}
                >
                  ₹{sec.lastPrice.toFixed(2)}
                </Typography>
                <Typography
                  sx={{
                    fontSize: '0.625rem',
                    fontWeight: 500,
                    color: isPositive ? '#26a69a' : isNegative ? '#ef5350' : '#787b86',
                  }}
                >
                  {isPositive ? '+' : ''}{sec.changePercent.toFixed(2)}%
                </Typography>
              </Box>
            );
          })}
        </Box>

        {/* Chart header */}
        {activeSecurity && (
          <Box
            sx={{
              px: 2,
              py: 1.5,
              borderBottom: '1px solid',
              borderColor: 'divider',
              display: 'flex',
              alignItems: 'baseline',
              gap: 2,
              bgcolor: '#1e222d',
            }}
          >
            <Typography sx={{ fontSize: '1.125rem', fontWeight: 700, color: '#fff' }}>
              {activeSecurity.symbol}
            </Typography>
            <Typography
              sx={{
                fontSize: '1.125rem',
                fontWeight: 700,
                fontFamily: "'JetBrains Mono', monospace",
                color: activeSecurity.change >= 0 ? '#26a69a' : '#ef5350',
              }}
            >
              ₹{activeSecurity.lastPrice.toFixed(2)}
            </Typography>
            <Box
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.5,
                px: 1,
                py: 0.25,
                borderRadius: '3px',
                bgcolor: activeSecurity.change >= 0 ? 'rgba(38, 166, 154, 0.12)' : 'rgba(239, 83, 80, 0.12)',
              }}
            >
              <Typography
                sx={{
                  fontSize: '0.75rem',
                  fontWeight: 600,
                  color: activeSecurity.change >= 0 ? '#26a69a' : '#ef5350',
                }}
              >
                {activeSecurity.change >= 0 ? '+' : ''}{activeSecurity.change.toFixed(2)}
                {' '}({activeSecurity.change >= 0 ? '+' : ''}{activeSecurity.changePercent.toFixed(2)}%)
              </Typography>
            </Box>
            <Typography sx={{ fontSize: '0.6875rem', color: '#5d606b', ml: 'auto' }}>
              Vol: {activeSecurity.volume.toLocaleString()}
            </Typography>
            <Typography sx={{ fontSize: '0.6875rem', color: '#5d606b' }}>
              1m Candlestick
            </Typography>
          </Box>
        )}

        {/* Main chart */}
        <Box sx={{ flex: 1, position: 'relative', minHeight: 0 }}>
          {error && (
            <Alert severity="warning" sx={{ m: 2, borderRadius: 1 }}>
              {error}
            </Alert>
          )}

          {isConnected && securitiesArray.length === 0 && (
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', flexDirection: 'column', gap: 2 }}>
              <CircularProgress size={32} sx={{ color: '#2962ff' }} />
              <Typography sx={{ color: '#787b86', fontSize: '0.8125rem' }}>
                Waiting for market data...
              </Typography>
            </Box>
          )}

          {!isConnected && securitiesArray.length === 0 && !error && (
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', flexDirection: 'column', gap: 2 }}>
              <CircularProgress size={32} sx={{ color: '#787b86' }} />
              <Typography sx={{ color: '#787b86', fontSize: '0.8125rem' }}>
                Connecting to market data stream...
              </Typography>
            </Box>
          )}

          {activeSecurity && (
            <CandlestickChart
              data={activeHistory || []}
              symbol={activeSecurity.symbol}
              fullWidth
            />
          )}
        </Box>

        {/* Status bar */}
        <Box className="status-bar">
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
            <span className={`live-indicator ${isConnected ? '' : 'disconnected'}`} />
            <Typography component="span" sx={{ fontSize: '0.6875rem', color: isConnected ? '#26a69a' : '#ef5350' }}>
              {isConnected ? 'LIVE' : 'DISCONNECTED'}
            </Typography>
          </Box>
          <Typography component="span" sx={{ fontSize: '0.6875rem', color: '#5d606b' }}>
            SSE Stream
          </Typography>
          <Typography component="span" sx={{ fontSize: '0.6875rem', color: '#5d606b' }}>
            {securitiesArray.length} symbols
          </Typography>
          <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography component="span" sx={{ fontSize: '0.6875rem', color: '#5d606b' }}>
              .NET Aspire • Redis Streams • React
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* === Watchlist Sidebar === */}
      <Box
        sx={{
          width: 280,
          flexShrink: 0,
          display: 'flex',
          flexDirection: 'column',
          bgcolor: '#1e222d',
          overflow: 'hidden',
        }}
      >
        {/* Watchlist header */}
        <Box
          sx={{
            px: 1.5,
            py: 1,
            borderBottom: '1px solid',
            borderColor: 'divider',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Typography variant="subtitle2" sx={{ color: '#787b86', fontSize: '0.6875rem' }}>
            WATCHLIST
          </Typography>
          <Typography variant="caption" sx={{ color: '#5d606b' }}>
            {securitiesArray.length} symbols
          </Typography>
        </Box>

        {/* Column headers */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: '1fr auto auto',
            gap: 1,
            px: 1.5,
            py: 0.75,
            borderBottom: '1px solid',
            borderColor: 'divider',
            bgcolor: 'rgba(42, 46, 57, 0.5)',
          }}
        >
          <Typography sx={{ fontSize: '0.625rem', color: '#5d606b', textTransform: 'uppercase', fontWeight: 600, letterSpacing: '0.05em' }}>
            Symbol
          </Typography>
          <Typography sx={{ fontSize: '0.625rem', color: '#5d606b', textTransform: 'uppercase', fontWeight: 600, letterSpacing: '0.05em', textAlign: 'right', minWidth: 70 }}>
            Last
          </Typography>
          <Typography sx={{ fontSize: '0.625rem', color: '#5d606b', textTransform: 'uppercase', fontWeight: 600, letterSpacing: '0.05em', textAlign: 'right', minWidth: 60 }}>
            Chg%
          </Typography>
        </Box>

        {/* Securities list */}
        <Box sx={{ flex: 1, overflowY: 'auto' }}>
          {securitiesArray.map((security) => (
            <StockCard
              key={security.symbol}
              security={security}
              priceHistory={priceHistory.get(security.symbol)}
              isSelected={security.symbol === activeSymbol}
              onSelect={() => setSelectedSymbol(security.symbol)}
            />
          ))}
        </Box>
      </Box>
    </Box>
  );
}
