# Task 12: Create Stock Card UI Components

## Objective
Build Material UI components to display real-time stock information with visual indicators.

## Prerequisites
- Task 11 completed (useMarketData hook working)
- Material UI installed and configured

## Acceptance Criteria
- [ ] StockCard component created
- [ ] Displays symbol, price, volume, timestamp
- [ ] Color-coded price changes (green/red)
- [ ] Price change percentage shown
- [ ] Trend indicators (up/down arrows)
- [ ] Responsive grid layout
- [ ] Professional styling with Material UI theme
- [ ] Real-time updates without flicker
- [ ] Accessible (ARIA labels)

## Implementation

### src/WebApp/src/components/StockCard.tsx
```typescript
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
import { Security } from '../types';
import { format } from 'date-fns';

interface StockCardProps {
  security: Security;
}

export const StockCard: React.FC<StockCardProps> = ({ security }) => {
  const isPositive = security.change > 0;
  const isNegative = security.change < 0;
  const isNeutral = security.change === 0;

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
          {/* Symbol */}
          <Box display="flex" justifyContent="space-between" alignItems="center">
            <Typography variant="h5" component="h2" fontWeight="bold">
              {security.symbol}
            </Typography>
            <TrendIcon 
              sx={{ color: priceColor, fontSize: 28 }}
              aria-label={isPositive ? 'trending up' : isNegative ? 'trending down' : 'neutral'}
            />
          </Box>

          {/* Price */}
          <Box>
            <Typography 
              variant="h4" 
              component="div" 
              sx={{ color: priceColor, fontWeight: 'bold' }}
            >
              ₹{security.lastPrice.toFixed(2)}
            </Typography>
          </Box>

          {/* Change */}
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

          {/* Volume */}
          <Box>
            <Typography variant="body2" color="text.secondary">
              Volume: {security.volume.toLocaleString()}
            </Typography>
          </Box>

          {/* Timestamp */}
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
```

### Update App.tsx to Display Cards
```typescript
import React from 'react';
import {
  Container,
  Typography,
  Box,
  Alert,
  Grid,
  CircularProgress,
} from '@mui/material';
import { useMarketData } from './hooks/useMarketData';
import { StockCard } from './components/StockCard';

function App() {
  const brokerUrl = process.env.VITE_BROKER_URL || 'http://localhost:5002';
  const { securities, isConnected, error } = useMarketData(brokerUrl);

  const securitiesArray = Array.from(securities.values());

  return (
    <Container maxWidth="lg">
      <Box sx={{ my: 4 }}>
        {/* Header */}
        <Typography variant="h3" component="h1" gutterBottom>
          Stock Market Simulator
        </Typography>
        <Typography variant="subtitle1" color="text.secondary" gutterBottom>
          Real-time market data streaming
        </Typography>

        {/* Status Alerts */}
        {error && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {isConnected && securitiesArray.length === 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
            <CircularProgress />
            <Typography sx={{ ml: 2 }}>
              Waiting for market data...
            </Typography>
          </Box>
        )}

        {/* Stock Cards Grid */}
        {securitiesArray.length > 0 && (
          <Grid container spacing={3} sx={{ mt: 2 }}>
            {securitiesArray.map((security) => (
              <Grid item xs={12} sm={6} md={4} lg={3} key={security.symbol}>
                <StockCard security={security} />
              </Grid>
            ))}
          </Grid>
        )}

        {/* Footer */}
        {isConnected && (
          <Box sx={{ mt: 4, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              Tracking {securitiesArray.length} securities in real-time
            </Typography>
          </Box>
        )}
      </Box>
    </Container>
  );
}

export default App;
```

### Add Custom CSS (App.css)
```css
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
}

.price-update {
  animation: pulse 0.5s ease-in-out;
}

body {
  background-color: #f5f5f5;
}
```

## Verification

### 1. Visual Test
1. Start all services
2. Open app at http://localhost:3000
3. Expected display:
   - Grid of stock cards
   - Real-time price updates
   - Green/red color indicators
   - Up/down arrows
   - Smooth animations

### 2. Responsive Test
1. Resize browser window
2. Verify grid adapts:
   - Mobile: 1 column
   - Tablet: 2 columns
   - Desktop: 3-4 columns

### 3. Interaction Test
1. Hover over cards → slight lift effect
2. Watch prices update → smooth transitions
3. Verify no UI flickering

### 4. Data Accuracy Test
1. Check a specific symbol
2. Verify:
   - Price matches Simulator output
   - Change calculation is correct
   - Percentage is accurate
   - Volume updates properly

### 5. Accessibility Test
1. Use keyboard navigation (Tab key)
2. Check screen reader announces trend direction
3. Verify color contrast meets WCAG standards

## Related Sections
- Technical Approach Section 5.5 (Web UI - StockCard)
- Technical Approach Section 7 (Phase 4: UI)

## Estimated Effort
2-3 hours
