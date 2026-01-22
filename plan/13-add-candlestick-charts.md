# Task 13: Add d3.js Candlestick Charts

## Objective
Implement interactive candlestick charts using d3.js to visualize price history.

## Prerequisites
- Task 12 completed (StockCard components working)
- d3.js installed
- Historical price data available (in-memory storage)

## Acceptance Criteria
- [ ] CandlestickChart component created
- [ ] d3.js properly integrated
- [ ] OHLC (Open, High, Low, Close) data calculated from ticks
- [ ] Candlestick visualization rendered correctly
- [ ] Time-based x-axis
- [ ] Price-based y-axis
- [ ] Interactive tooltips on hover
- [ ] Responsive chart sizing
- [ ] Smooth updates as new data arrives

## Implementation

### src/WebApp/src/components/CandlestickChart.tsx
```typescript
import React, { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import { Box, Paper } from '@mui/material';

interface OHLC {
  timestamp: Date;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

interface CandlestickChartProps {
  data: OHLC[];
  symbol: string;
  width?: number;
  height?: number;
}

export const CandlestickChart: React.FC<CandlestickChartProps> = ({
  data,
  symbol,
  width = 600,
  height = 300,
}) => {
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!svgRef.current || data.length === 0) return;

    const margin = { top: 20, right: 30, bottom: 30, left: 50 };
    const innerWidth = width - margin.left - margin.right;
    const innerHeight = height - margin.top - margin.bottom;

    // Clear previous chart
    d3.select(svgRef.current).selectAll('*').remove();

    const svg = d3.select(svgRef.current)
      .attr('width', width)
      .attr('height', height);

    const g = svg.append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    // Scales
    const xScale = d3.scaleTime()
      .domain(d3.extent(data, d => d.timestamp) as [Date, Date])
      .range([0, innerWidth]);

    const yScale = d3.scaleLinear()
      .domain([
        d3.min(data, d => d.low) as number * 0.999,
        d3.max(data, d => d.high) as number * 1.001,
      ])
      .range([innerHeight, 0]);

    // Axes
    g.append('g')
      .attr('transform', `translate(0,${innerHeight})`)
      .call(d3.axisBottom(xScale).ticks(5).tickFormat(d3.timeFormat('%H:%M') as any));

    g.append('g')
      .call(d3.axisLeft(yScale).ticks(5));

    // Candlesticks
    const candleWidth = Math.min(innerWidth / data.length * 0.8, 10);

    data.forEach((d) => {
      const x = xScale(d.timestamp);
      const isGreen = d.close >= d.open;
      const color = isGreen ? '#4caf50' : '#f44336';

      // High-Low line
      g.append('line')
        .attr('x1', x)
        .attr('x2', x)
        .attr('y1', yScale(d.high))
        .attr('y2', yScale(d.low))
        .attr('stroke', color)
        .attr('stroke-width', 1);

      // Open-Close body
      g.append('rect')
        .attr('x', x - candleWidth / 2)
        .attr('y', yScale(Math.max(d.open, d.close)))
        .attr('width', candleWidth)
        .attr('height', Math.abs(yScale(d.open) - yScale(d.close)) || 1)
        .attr('fill', color)
        .attr('stroke', color);
    });

  }, [data, width, height, symbol]);

  return (
    <Paper sx={{ p: 2, mt: 2 }}>
      <Box>
        <svg ref={svgRef}></svg>
      </Box>
    </Paper>
  );
};
```

### Update StockCard to Include Chart
```typescript
import React, { useState } from 'react';
import { Card, CardContent, Typography, Box, Button } from '@mui/material';
import { ShowChart } from '@mui/icons-material';
import { CandlestickChart } from './CandlestickChart';

export const StockCard: React.FC<StockCardProps> = ({ security, priceHistory }) => {
  const [showChart, setShowChart] = useState(false);

  return (
    <Card>
      <CardContent>
        {/* ... existing card content ... */}
        
        <Button 
          startIcon={<ShowChart />}
          onClick={() => setShowChart(!showChart)}
          size="small"
        >
          {showChart ? 'Hide' : 'Show'} Chart
        </Button>

        {showChart && priceHistory && (
          <CandlestickChart 
            data={priceHistory} 
            symbol={security.symbol}
          />
        )}
      </CardContent>
    </Card>
  );
};
```

## Data Aggregation Strategy

To create OHLC data from ticks, implement a time-window aggregator:

```typescript
// src/WebApp/src/utils/priceAggregator.ts
export class PriceAggregator {
  private tickWindow: MarketDataEvent[] = [];
  private windowSize = 60000; // 1 minute

  addTick(tick: MarketDataEvent): OHLC | null {
    this.tickWindow.push(tick);
    
    const oldestAllowed = Date.now() - this.windowSize;
    this.tickWindow = this.tickWindow.filter(
      t => new Date(t.timestamp).getTime() > oldestAllowed
    );

    if (this.tickWindow.length < 2) return null;

    return {
      timestamp: new Date(tick.timestamp),
      open: this.tickWindow[0].price,
      high: Math.max(...this.tickWindow.map(t => t.price)),
      low: Math.min(...this.tickWindow.map(t => t.price)),
      close: tick.price,
      volume: this.tickWindow.reduce((sum, t) => sum + t.volume, 0),
    };
  }
}
```

## Verification

### 1. Visual Test
1. Click "Show Chart" button on any stock card
2. Verify candlestick chart appears
3. Check:
   - Green candles for price increases
   - Red candles for price decreases
   - Wicks show high/low correctly
   - Axes labeled properly

### 2. Data Accuracy Test
1. Compare chart data with raw tick data
2. Verify OHLC calculations are correct
3. Check time windows are accurate

### 3. Responsive Test
1. Resize browser window
2. Verify chart scales appropriately

### 4. Performance Test
1. Open charts for multiple symbols
2. Monitor browser performance
3. Check for memory leaks over 5 minutes

## Related Sections
- Technical Approach Section 2 (Technology Stack - d3.js)
- Technical Approach Section 7 (Phase 5: Polish)

## Estimated Effort
4-5 hours
