import React, { useCallback, useState } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  Stack,
  Button,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  TrendingFlat,
  ShowChart,
} from '@mui/icons-material';
import type { Security } from '../types';
import type { OHLC } from '../utils/priceAggregator';
import { format } from 'date-fns';
import { CandlestickChart } from './CandlestickChart';

interface StockCardProps {
  security: Security;
  priceHistory?: OHLC[];
}

export const StockCard: React.FC<StockCardProps> = ({ security, priceHistory }) => {
  const [showChart, setShowChart] = useState(false);
  const isPositive = security.change > 0;
  const isNegative = security.change < 0;

  const priceColor = isPositive ? 'success.main' : isNegative ? 'error.main' : 'text.primary';
  const bgColor = isPositive ? 'success.light' : isNegative ? 'error.light' : 'grey.100';

  const TrendIcon = isPositive ? TrendingUp : isNegative ? TrendingDown : TrendingFlat;

  const formatTimestamp = useCallback((timestamp: string) => {
    try {
      return format(new Date(timestamp), 'HH:mm:ss');
    } catch {
      return timestamp;
    }
  }, []);

  return (
    <Card 
      sx={{ 
        height: '100%',
        transition: 'all 0.3s ease',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: 4,
        },
      }}
    >
      <CardContent>
        <Stack spacing={2}>
          <Box display="flex" justifyContent="space-between" alignItems="center">
            <Typography variant="h5" component="h2" fontWeight="bold">
              {security.symbol}
            </Typography>
            <TrendIcon 
              sx={{ color: priceColor, fontSize: 28 }}
              aria-label={isPositive ? 'trending up' : isNegative ? 'trending down' : 'neutral'}
            />
          </Box>

          <Box>
            <Typography 
              variant="h4" 
              component="div" 
              sx={{ color: priceColor, fontWeight: 'bold' }}
            >
              ₹{(security.lastPrice ?? 0).toFixed(2)}
            </Typography>
          </Box>

          <Box>
            <Chip
              label={`${isPositive ? '+' : ''}${(security.change ?? 0).toFixed(2)} (${isPositive ? '+' : ''}${(security.changePercent ?? 0).toFixed(2)}%)`}
              size="small"
              sx={{
                backgroundColor: bgColor,
                color: priceColor,
                fontWeight: 'bold',
              }}
            />
          </Box>

          <Box>
            <Typography variant="body2" color="text.secondary">
              Volume: {security.volume.toLocaleString()}
            </Typography>
          </Box>

          <Box>
            <Typography variant="caption" color="text.secondary">
              Updated: {formatTimestamp(security.lastUpdate)}
            </Typography>
          </Box>

          <Box mt={1}>
            <Button 
              startIcon={<ShowChart />}
              onClick={() => setShowChart(!showChart)}
              size="small"
              variant="outlined"
              fullWidth
            >
              {showChart ? 'Hide' : 'Show'} Chart
            </Button>
          </Box>
        </Stack>
      </CardContent>

      {showChart && priceHistory && priceHistory.length > 0 && (
        <CandlestickChart 
          data={priceHistory} 
          symbol={security.symbol}
        />
      )}
    </Card>
  );
};
