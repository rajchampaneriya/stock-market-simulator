import React, { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import { Box, Typography, Tooltip } from '@mui/material';
import type { OptionsSnapshot } from '../../types/options';

const COLORS = {
  bg: '#131722',
  grid: 'rgba(42, 46, 57, 0.5)',
  gridText: '#5d606b',
  expiry: '#26a69a',
  today: '#2962ff',
  zero: '#787b86',
  marker: '#ff9800',
  axisLine: '#2a2e39',
};

interface Props {
  snapshot: OptionsSnapshot;
  height?: number;
}

/**
 * Payoff diagram: P/L at expiry and today (T+0) across spot,
 * with the current spot marked.
 */
export const PayoffDiagram: React.FC<Props> = ({ snapshot, height = 300 }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!svgRef.current || !containerRef.current || snapshot.payoffExpiry.length < 2) return;
    const rect = containerRef.current.getBoundingClientRect();
    const width = rect.width;
    if (width <= 0) return;

    const margin = { top: 14, right: 66, bottom: 26, left: 10 };
    const innerW = width - margin.left - margin.right;
    const innerH = height - margin.top - margin.bottom;

    d3.select(svgRef.current).selectAll('*').remove();
    const svg = d3
      .select(svgRef.current)
      .attr('width', width)
      .attr('height', height)
      .style('display', 'block');
    svg.append('rect').attr('width', width).attr('height', height).attr('fill', COLORS.bg);
    const g = svg.append('g').attr('transform', `translate(${margin.left},${margin.top})`);

    const all = [...snapshot.payoffExpiry, ...snapshot.payoffToday];
    const xScale = d3
      .scaleLinear()
      .domain(d3.extent(all, (p) => p.spot) as [number, number])
      .range([0, innerW]);
    const yExtent = d3.extent(all, (p) => p.pnl) as [number, number];
    const yScale = d3
      .scaleLinear()
      .domain([yExtent[0] * 1.1, Math.max(yExtent[1] * 1.1, 1)])
      .range([innerH, 0]);

    g.append('g')
      .selectAll('line')
      .data(yScale.ticks(5))
      .join('line')
      .attr('x1', 0)
      .attr('x2', innerW)
      .attr('y1', (d) => yScale(d))
      .attr('y2', (d) => yScale(d))
      .attr('stroke', COLORS.grid);
    g.append('g')
      .attr('transform', `translate(0,${innerH})`)
      .call(d3.axisBottom(xScale).ticks(6).tickSize(0))
      .call((s) => s.select('domain').attr('stroke', COLORS.axisLine))
      .call((s) => s.selectAll('text').attr('fill', COLORS.gridText).attr('font-size', 10));
    g.append('g')
      .attr('transform', `translate(${innerW},0)`)
      .call(d3.axisRight(yScale).ticks(5).tickFormat(d3.format('~s')).tickSize(0))
      .call((s) => s.select('domain').attr('stroke', COLORS.axisLine))
      .call((s) => s.selectAll('text').attr('fill', COLORS.gridText).attr('font-size', 10));

    // Zero line
    g.append('line')
      .attr('x1', 0)
      .attr('x2', innerW)
      .attr('y1', yScale(0))
      .attr('y2', yScale(0))
      .attr('stroke', COLORS.zero)
      .attr('stroke-dasharray', '3 3');

    const line = d3
      .line<{ spot: number; pnl: number }>()
      .x((p) => xScale(p.spot))
      .y((p) => yScale(p.pnl));

    g.append('path')
      .datum(snapshot.payoffExpiry)
      .attr('fill', 'none')
      .attr('stroke', COLORS.expiry)
      .attr('stroke-width', 1.8)
      .attr('d', line);
    g.append('path')
      .datum(snapshot.payoffToday)
      .attr('fill', 'none')
      .attr('stroke', COLORS.today)
      .attr('stroke-width', 1.4)
      .attr('stroke-dasharray', '5 3')
      .attr('d', line);

    // Current spot marker
    g.append('line')
      .attr('x1', xScale(snapshot.spot))
      .attr('x2', xScale(snapshot.spot))
      .attr('y1', 0)
      .attr('y2', innerH)
      .attr('stroke', COLORS.marker)
      .attr('stroke-width', 1);
    g.append('text')
      .attr('x', xScale(snapshot.spot) + 4)
      .attr('y', 12)
      .attr('fill', COLORS.marker)
      .attr('font-size', 9)
      .text(`spot ${snapshot.spot.toFixed(0)}`);
  }, [snapshot, height]);

  return (
    <Box ref={containerRef} sx={{ width: '100%' }}>
      <Box sx={{ display: 'flex', gap: 2, mb: 0.5, px: 1 }}>
        <Typography variant="caption" sx={{ color: COLORS.expiry }}>
          — P/L at expiry
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.today }}>
          – – P/L today (T+0)
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.marker }}>
          | spot
        </Typography>
        <Tooltip title="At expiry the payoff is the pure intrinsic value of the structure. The T+0 curve re-prices every option at today's IV and time value — it shows what you would gain or lose if you closed everything right now at each spot level." arrow>
          <Typography variant="caption" sx={{ cursor: 'help', textDecoration: 'underline dotted' }}>
            expiry vs T+0?
          </Typography>
        </Tooltip>
      </Box>
      <svg ref={svgRef} />
    </Box>
  );
};
