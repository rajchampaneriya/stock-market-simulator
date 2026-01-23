import React, { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import { Box, Paper, Typography } from '@mui/material';
import type { OHLC } from '../utils/priceAggregator';

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

    d3.select(svgRef.current).selectAll('*').remove();

    const svg = d3.select(svgRef.current)
      .attr('width', width)
      .attr('height', height);

    const g = svg.append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const xScale = d3.scaleTime()
      .domain(d3.extent(data, d => d.timestamp) as [Date, Date])
      .range([0, innerWidth]);

    const yScale = d3.scaleLinear()
      .domain([
        d3.min(data, d => d.low) as number * 0.999,
        d3.max(data, d => d.high) as number * 1.001,
      ])
      .range([innerHeight, 0]);

    g.append('g')
      .attr('transform', `translate(0,${innerHeight})`)
      .call(d3.axisBottom(xScale).ticks(5).tickFormat((d) => {
        return d3.timeFormat('%H:%M')(d as Date);
      }));

    g.append('g')
      .call(d3.axisLeft(yScale).ticks(5));

    const candleWidth = Math.min(innerWidth / data.length * 0.8, 10);

    data.forEach((d) => {
      const x = xScale(d.timestamp);
      const isGreen = d.close >= d.open;
      const color = isGreen ? '#4caf50' : '#f44336';

      g.append('line')
        .attr('x1', x)
        .attr('x2', x)
        .attr('y1', yScale(d.high))
        .attr('y2', yScale(d.low))
        .attr('stroke', color)
        .attr('stroke-width', 1);

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
        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
          {symbol} - 1 Minute Candlestick Chart
        </Typography>
        <svg ref={svgRef}></svg>
      </Box>
    </Paper>
  );
};
