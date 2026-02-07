import React, { useCallback, useRef, useEffect } from 'react';
import {
  Typography,
  Box,
} from '@mui/material';
import type { Security } from '../types';
import type { OHLC } from '../utils/priceAggregator';
import { format } from 'date-fns';

interface StockCardProps {
  security: Security;
  priceHistory?: OHLC[];
  isSelected?: boolean;
  onSelect?: () => void;
}

/**
 * Compact watchlist row — TradingView style
 */
export const StockCard: React.FC<StockCardProps> = ({ security, priceHistory, isSelected, onSelect }) => {
  const isPositive = security.change > 0;
  const isNegative = security.change < 0;
  const rowRef = useRef<HTMLDivElement>(null);
  const prevPriceRef = useRef(security.lastPrice);

  const priceColor = isPositive ? '#26a69a' : isNegative ? '#ef5350' : '#d1d4dc';
  const changeBg = isPositive ? 'rgba(38, 166, 154, 0.12)' : isNegative ? 'rgba(239, 83, 80, 0.12)' : 'rgba(120, 123, 134, 0.08)';

  // Flash on price change
  useEffect(() => {
    if (prevPriceRef.current !== security.lastPrice && rowRef.current) {
      const flashClass = security.lastPrice > prevPriceRef.current ? 'price-flash-green' : 'price-flash-red';
      rowRef.current.classList.add(flashClass);
      const timer = setTimeout(() => rowRef.current?.classList.remove(flashClass), 600);
      prevPriceRef.current = security.lastPrice;
      return () => clearTimeout(timer);
    }
  }, [security.lastPrice]);

  const formatTimestamp = useCallback((timestamp: string) => {
    try {
      return format(new Date(timestamp), 'HH:mm:ss');
    } catch {
      return timestamp;
    }
  }, []);

  // Mini sparkline from price history
  const sparkline = React.useMemo(() => {
    if (!priceHistory || priceHistory.length < 2) return null;
    const prices = priceHistory.slice(-20).map(p => p.close);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const range = max - min || 1;
    const width = 48;
    const height = 20;
    const points = prices.map((p, i) => {
      const x = (i / (prices.length - 1)) * width;
      const y = height - ((p - min) / range) * height;
      return `${x},${y}`;
    }).join(' ');
    const lineColor = prices[prices.length - 1] >= prices[0] ? '#26a69a' : '#ef5350';
    return (
      <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} style={{ display: 'block' }}>
        <polyline
          points={points}
          fill="none"
          stroke={lineColor}
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }, [priceHistory]);

  return (
    <Box
      ref={rowRef}
      className={`watchlist-item ${isSelected ? 'selected' : ''}`}
      onClick={onSelect}
      sx={{
        display: 'grid',
        gridTemplateColumns: '1fr auto auto',
        gap: 1,
        px: 1.5,
        py: 1,
        alignItems: 'center',
        borderLeft: isSelected ? '2px solid #2962ff' : '2px solid transparent',
        bgcolor: isSelected ? 'rgba(41, 98, 255, 0.06)' : 'transparent',
        '&:hover': {
          bgcolor: 'rgba(255, 255, 255, 0.03)',
        },
        transition: 'background-color 0.15s ease',
        cursor: 'pointer',
        borderBottom: '1px solid rgba(42, 46, 57, 0.6)',
      }}
    >
      {/* Symbol + Sparkline + Time */}
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25, minWidth: 0 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography
            sx={{
              fontSize: '0.8125rem',
              fontWeight: 600,
              color: isSelected ? '#fff' : '#d1d4dc',
              letterSpacing: '-0.01em',
            }}
          >
            {security.symbol}
          </Typography>
          {sparkline}
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography sx={{ fontSize: '0.625rem', color: '#5d606b' }}>
            Vol: {security.volume.toLocaleString()}
          </Typography>
          <Typography sx={{ fontSize: '0.625rem', color: '#5d606b' }}>
            {formatTimestamp(security.lastUpdate)}
          </Typography>
        </Box>
      </Box>

      {/* Price */}
      <Typography
        sx={{
          fontSize: '0.8125rem',
          fontWeight: 600,
          fontFamily: "'JetBrains Mono', monospace",
          color: priceColor,
          textAlign: 'right',
          minWidth: 70,
          letterSpacing: '-0.01em',
        }}
      >
        {security.lastPrice.toFixed(2)}
      </Typography>

      {/* Change % pill */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          minWidth: 60,
        }}
      >
        <Box
          sx={{
            px: 0.75,
            py: 0.25,
            borderRadius: '3px',
            bgcolor: changeBg,
            textAlign: 'right',
          }}
        >
          <Typography
            sx={{
              fontSize: '0.6875rem',
              fontWeight: 600,
              fontFamily: "'JetBrains Mono', monospace",
              color: priceColor,
              lineHeight: 1.4,
            }}
          >
            {isPositive ? '+' : ''}{security.changePercent.toFixed(2)}%
          </Typography>
        </Box>
      </Box>
    </Box>
  );
};
