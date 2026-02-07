import React, { useEffect, useRef, useCallback } from 'react';
import * as d3 from 'd3';
import { Box, Typography } from '@mui/material';
import type { OHLC } from '../utils/priceAggregator';

interface CandlestickChartProps {
  data: OHLC[];
  symbol: string;
  width?: number;
  height?: number;
  fullWidth?: boolean;
}

const COLORS = {
  bg: '#131722',
  grid: 'rgba(42, 46, 57, 0.5)',
  gridText: '#5d606b',
  bullish: '#26a69a',
  bearish: '#ef5350',
  crosshair: '#787b86',
  tooltipBg: 'rgba(42, 46, 57, 0.95)',
  tooltipBorder: '#363a45',
  volumeBullish: 'rgba(38, 166, 154, 0.25)',
  volumeBearish: 'rgba(239, 83, 80, 0.25)',
  axisLine: '#2a2e39',
  priceLine: 'rgba(41, 98, 255, 0.4)',
  priceLineDash: 'rgba(41, 98, 255, 0.8)',
};

export const CandlestickChart: React.FC<CandlestickChartProps> = ({
  data,
  symbol,
  fullWidth = false,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);

  const renderChart = useCallback(() => {
    if (!svgRef.current || !containerRef.current || data.length === 0) return;

    const container = containerRef.current;
    const rect = container.getBoundingClientRect();
    const width = fullWidth ? rect.width : Math.min(rect.width, 800);
    const height = fullWidth ? rect.height : 300;

    if (width <= 0 || height <= 0) return;

    const margin = { top: 16, right: 72, bottom: 28, left: 12 };
    const volumeHeight = height * 0.18;
    const chartHeight = height - margin.top - margin.bottom - volumeHeight;
    const innerWidth = width - margin.left - margin.right;

    d3.select(svgRef.current).selectAll('*').remove();

    const svg = d3.select(svgRef.current)
      .attr('width', width)
      .attr('height', height)
      .style('display', 'block');

    // Background
    svg.append('rect')
      .attr('width', width)
      .attr('height', height)
      .attr('fill', COLORS.bg);

    const g = svg.append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    // Scales
    const xScale = d3.scaleBand<number>()
      .domain(data.map((_, i) => i))
      .range([0, innerWidth])
      .padding(0.3);

    const yMin = (d3.min(data, d => d.low) as number) * 0.9995;
    const yMax = (d3.max(data, d => d.high) as number) * 1.0005;

    const yScale = d3.scaleLinear()
      .domain([yMin, yMax])
      .range([chartHeight, 0])
      .nice();

    const volMax = d3.max(data, d => d.volume) as number || 1;
    const volScale = d3.scaleLinear()
      .domain([0, volMax])
      .range([0, volumeHeight - 4]);

    const timeScale = d3.scaleTime()
      .domain(d3.extent(data, d => d.timestamp) as [Date, Date])
      .range([0, innerWidth]);

    // --- Grid Lines ---
    const yTicks = yScale.ticks(6);
    yTicks.forEach(tick => {
      g.append('line')
        .attr('x1', 0)
        .attr('x2', innerWidth)
        .attr('y1', yScale(tick))
        .attr('y2', yScale(tick))
        .attr('stroke', COLORS.grid)
        .attr('stroke-width', 1);
    });

    const xTickInterval = Math.max(1, Math.floor(data.length / 6));
    data.forEach((d, i) => {
      if (i % xTickInterval === 0) {
        const x = (xScale(i) ?? 0) + (xScale.bandwidth() / 2);
        g.append('line')
          .attr('x1', x)
          .attr('x2', x)
          .attr('y1', 0)
          .attr('y2', chartHeight)
          .attr('stroke', COLORS.grid)
          .attr('stroke-width', 1);
      }
    });

    // --- Price axis (right) ---
    yTicks.forEach(tick => {
      g.append('text')
        .attr('x', innerWidth + 8)
        .attr('y', yScale(tick))
        .attr('dy', '0.35em')
        .attr('fill', COLORS.gridText)
        .attr('font-size', '10px')
        .attr('font-family', "'JetBrains Mono', monospace")
        .text(tick.toFixed(2));
    });

    // --- Time axis (bottom) ---
    data.forEach((d, i) => {
      if (i % xTickInterval === 0) {
        const x = (xScale(i) ?? 0) + (xScale.bandwidth() / 2);
        g.append('text')
          .attr('x', x)
          .attr('y', chartHeight + volumeHeight + 14)
          .attr('text-anchor', 'middle')
          .attr('fill', COLORS.gridText)
          .attr('font-size', '10px')
          .attr('font-family', "'JetBrains Mono', monospace")
          .text(d3.timeFormat('%H:%M')(d.timestamp));
      }
    });

    // --- Volume Bars ---
    const volGroup = g.append('g')
      .attr('transform', `translate(0,${chartHeight})`);

    data.forEach((d, i) => {
      const x = xScale(i) ?? 0;
      const barHeight = volScale(d.volume);
      const isGreen = d.close >= d.open;

      volGroup.append('rect')
        .attr('x', x)
        .attr('y', volumeHeight - barHeight)
        .attr('width', xScale.bandwidth())
        .attr('height', barHeight)
        .attr('fill', isGreen ? COLORS.volumeBullish : COLORS.volumeBearish)
        .attr('rx', 1);
    });

    // Separator line between chart and volume
    g.append('line')
      .attr('x1', 0)
      .attr('x2', innerWidth)
      .attr('y1', chartHeight)
      .attr('y2', chartHeight)
      .attr('stroke', COLORS.axisLine)
      .attr('stroke-width', 1);

    // --- Candlesticks ---
    const candleGroup = g.append('g');

    data.forEach((d, i) => {
      const x = (xScale(i) ?? 0) + xScale.bandwidth() / 2;
      const isGreen = d.close >= d.open;
      const color = isGreen ? COLORS.bullish : COLORS.bearish;

      // Wick
      candleGroup.append('line')
        .attr('x1', x)
        .attr('x2', x)
        .attr('y1', yScale(d.high))
        .attr('y2', yScale(d.low))
        .attr('stroke', color)
        .attr('stroke-width', 1);

      // Body
      const bodyTop = yScale(Math.max(d.open, d.close));
      const bodyBottom = yScale(Math.min(d.open, d.close));
      const bodyHeight = Math.max(bodyBottom - bodyTop, 1);

      candleGroup.append('rect')
        .attr('x', xScale(i) ?? 0)
        .attr('y', bodyTop)
        .attr('width', xScale.bandwidth())
        .attr('height', bodyHeight)
        .attr('fill', color)
        .attr('rx', 1);
    });

    // --- Current price line (dashed) ---
    const lastPrice = data[data.length - 1].close;
    const lastPriceY = yScale(lastPrice);

    g.append('line')
      .attr('x1', 0)
      .attr('x2', innerWidth)
      .attr('y1', lastPriceY)
      .attr('y2', lastPriceY)
      .attr('stroke', COLORS.priceLine)
      .attr('stroke-width', 1)
      .attr('stroke-dasharray', '4,3');

    // Price label on right
    const lastIsGreen = data[data.length - 1].close >= data[data.length - 1].open;
    g.append('rect')
      .attr('x', innerWidth + 2)
      .attr('y', lastPriceY - 10)
      .attr('width', 60)
      .attr('height', 20)
      .attr('fill', lastIsGreen ? COLORS.bullish : COLORS.bearish)
      .attr('rx', 2);

    g.append('text')
      .attr('x', innerWidth + 32)
      .attr('y', lastPriceY)
      .attr('dy', '0.35em')
      .attr('text-anchor', 'middle')
      .attr('fill', '#fff')
      .attr('font-size', '10px')
      .attr('font-weight', '600')
      .attr('font-family', "'JetBrains Mono', monospace")
      .text(lastPrice.toFixed(2));

    // --- Crosshair overlay ---
    const crosshairGroup = g.append('g').style('display', 'none');

    const crosshairV = crosshairGroup.append('line')
      .attr('y1', 0)
      .attr('y2', chartHeight + volumeHeight)
      .attr('stroke', COLORS.crosshair)
      .attr('stroke-width', 0.5)
      .attr('stroke-dasharray', '3,3');

    const crosshairH = crosshairGroup.append('line')
      .attr('x1', 0)
      .attr('x2', innerWidth)
      .attr('stroke', COLORS.crosshair)
      .attr('stroke-width', 0.5)
      .attr('stroke-dasharray', '3,3');

    // Tooltip
    const tooltip = crosshairGroup.append('g');
    const tooltipRect = tooltip.append('rect')
      .attr('fill', COLORS.tooltipBg)
      .attr('stroke', COLORS.tooltipBorder)
      .attr('stroke-width', 1)
      .attr('rx', 3)
      .attr('height', 72)
      .attr('width', 140);

    const tooltipText = tooltip.append('text')
      .attr('fill', '#d1d4dc')
      .attr('font-size', '10px')
      .attr('font-family', "'JetBrains Mono', monospace");

    // Price label on crosshair (right axis)
    const crosshairPriceRect = crosshairGroup.append('rect')
      .attr('fill', '#363a45')
      .attr('rx', 2)
      .attr('height', 18)
      .attr('width', 56);

    const crosshairPriceText = crosshairGroup.append('text')
      .attr('fill', '#d1d4dc')
      .attr('font-size', '10px')
      .attr('font-family', "'JetBrains Mono', monospace")
      .attr('text-anchor', 'middle');

    // Mouse overlay
    svg.append('rect')
      .attr('width', width)
      .attr('height', height)
      .attr('fill', 'transparent')
      .style('cursor', 'crosshair')
      .on('mouseenter', () => crosshairGroup.style('display', null))
      .on('mouseleave', () => crosshairGroup.style('display', 'none'))
      .on('mousemove', function(event) {
        const [mx, my] = d3.pointer(event);
        const adjustedX = mx - margin.left;
        const adjustedY = my - margin.top;

        // Find closest candle
        const step = innerWidth / data.length;
        const idx = Math.min(Math.max(Math.floor(adjustedX / step), 0), data.length - 1);
        const d = data[idx];
        if (!d) return;

        const candleX = (xScale(idx) ?? 0) + xScale.bandwidth() / 2;

        crosshairV.attr('x1', candleX).attr('x2', candleX);
        crosshairH.attr('y1', adjustedY).attr('y2', adjustedY);

        // Price label
        const hoveredPrice = yScale.invert(adjustedY);
        crosshairPriceRect
          .attr('x', innerWidth + 4)
          .attr('y', adjustedY - 9);
        crosshairPriceText
          .attr('x', innerWidth + 32)
          .attr('y', adjustedY + 4)
          .text(hoveredPrice.toFixed(2));

        // OHLC Tooltip
        const isGreen = d.close >= d.open;
        const tooltipX = candleX + 16 > innerWidth - 160 ? candleX - 156 : candleX + 16;
        const tooltipY = Math.min(Math.max(adjustedY - 36, 0), chartHeight - 78);

        tooltipRect.attr('x', tooltipX).attr('y', tooltipY);
        tooltipText
          .attr('x', tooltipX + 8)
          .attr('y', tooltipY + 14);

        tooltipText.selectAll('tspan').remove();
        const lines = [
          { label: 'O', value: d.open.toFixed(2), color: isGreen ? COLORS.bullish : COLORS.bearish },
          { label: 'H', value: d.high.toFixed(2), color: '#d1d4dc' },
          { label: 'L', value: d.low.toFixed(2), color: '#d1d4dc' },
          { label: 'C', value: d.close.toFixed(2), color: isGreen ? COLORS.bullish : COLORS.bearish },
          { label: 'V', value: d.volume.toLocaleString(), color: '#787b86' },
        ];
        lines.forEach((line, li) => {
          tooltipText.append('tspan')
            .attr('x', tooltipX + 8)
            .attr('dy', li === 0 ? 0 : 13)
            .attr('fill', '#787b86')
            .text(`${line.label}: `);
          tooltipText.append('tspan')
            .attr('fill', line.color)
            .text(line.value);
        });
      });

  }, [data, symbol, fullWidth]);

  // Render on data change
  useEffect(() => {
    renderChart();
  }, [renderChart]);

  // Resize observer
  useEffect(() => {
    if (!containerRef.current || !fullWidth) return;
    const observer = new ResizeObserver(() => renderChart());
    observer.observe(containerRef.current);
    return () => observer.disconnect();
  }, [renderChart, fullWidth]);

  if (data.length === 0) {
    return (
      <Box
        ref={containerRef}
        sx={{
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: COLORS.bg,
        }}
      >
        <Typography sx={{ color: '#5d606b', fontSize: '0.8125rem' }}>
          Accumulating price data for {symbol}...
        </Typography>
      </Box>
    );
  }

  return (
    <Box
      ref={containerRef}
      sx={{
        width: '100%',
        height: fullWidth ? '100%' : 300,
        bgcolor: COLORS.bg,
        overflow: 'hidden',
        position: 'relative',
      }}
    >
      <svg ref={svgRef} style={{ display: 'block' }} />
    </Box>
  );
};
