import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  Stack,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  TrendingFlat,
} from '@mui/icons-material';
import type { Security } from '../types';
import { format } from 'date-fns';

interface StockCardProps {
  security: Security;
}

export const StockCard: React.FC<StockCardProps> = ({ security }) => {
  const isPositive = security.change > 0;
  const isNegative = security.change < 0;

  const priceColor = isPositive ? 'success.main' : isNegative ? 'error.main' : 'text.primary';
  const bgColor = isPositive ? 'success.light' : isNegative ? 'error.light' : 'grey.100';

  const TrendIcon = isPositive ? TrendingUp : isNegative ? TrendingDown : TrendingFlat;

  const formatTimestamp = (timestamp: string) => {
    try {
      return format(new Date(timestamp), 'HH:mm:ss');
    } catch {
      return timestamp;
    }
  };

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
              ₹{security.lastPrice.toFixed(2)}
            </Typography>
          </Box>

          <Box>
            <Chip
              label={`${isPositive ? '+' : ''}${security.change.toFixed(2)} (${isPositive ? '+' : ''}${security.changePercent.toFixed(2)}%)`}
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
        </Stack>
      </CardContent>
    </Card>
  );
};
