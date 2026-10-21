import React, { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import { Box, Typography, Tooltip } from '@mui/material';
import type { OptionsSnapshot } from '../../types/options';

const COLORS = {
  bg: '#131722',
  grid: 'rgba(42, 46, 57, 0.5)',
  gridText: '#5d606b',
  line: '#2962ff',
  atm: '#ff9800',
  wing: 'rgba(239, 83, 80, 0.7)',
  breakeven: 'rgba(124, 77, 255, 0.7)',
  spike: '#ef5350',
  axisLine: '#2a2e39',
};

interface Props {
  snapshot: OptionsSnapshot;
  height?: number;
}

/**
 * NIFTY spot path with ATM strike, sold strikes, breakevens overlaid
 * and event-day spikes marked.
 */
export const NiftySpotChart: React.FC<Props> = ({ snapshot, height = 300 }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!svgRef.current || !containerRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const width = rect.width;
    if (width <= 0 || snapshot.spotHistory.length < 2) return;

    const margin = { top: 14, right: 66, bottom: 24, left: 10 };
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

    const points = snapshot.spotHistory;
    const soldStrikes = snapshot.legs.map((l) => l.strike);
    const levels = [
      ...soldStrikes,
      ...snapshot.breakevens,
      ...points.map((p) => p.spot),
    ];
    const yMin = (d3.min(levels) as number) - 30;
    const yMax = (d3.max(levels) as number) + 30;

    const xScale = d3
      .scaleLinear()
      .domain(d3.extent(points, (p) => p.tick) as [number, number])
      .range([0, innerW]);
    const yScale = d3.scaleLinear().domain([yMin, yMax]).range([innerH, 0]);

    // Grid
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
      .call((sel) => sel.select('domain').attr('stroke', COLORS.axisLine))
      .call((sel) => sel.selectAll('text').attr('fill', COLORS.gridText).attr('font-size', 10));
    g.append('g')
      .attr('transform', `translate(${innerW},0)`)
      .call(d3.axisRight(yScale).ticks(5).tickSize(0))
      .call((sel) => sel.select('domain').attr('stroke', COLORS.axisLine))
      .call((sel) => sel.selectAll('text').attr('fill', COLORS.gridText).attr('font-size', 10));

    // Overlaid levels: sold strikes (red), ATM (orange), breakevens (purple)
    const drawLevel = (value: number, color: string, label: string, dash = '4 3') => {
      g.append('line')
        .attr('x1', 0)
        .attr('x2', innerW)
        .attr('y1', yScale(value))
        .attr('y2', yScale(value))
        .attr('stroke', color)
        .attr('stroke-dasharray', dash)
        .attr('stroke-width', 1);
      g.append('text')
        .attr('x', 4)
        .attr('y', yScale(value) - 3)
        .attr('fill', color)
        .attr('font-size', 9)
        .text(label);
    };

    const uniqueStrikes = [...new Set(soldStrikes)];
    uniqueStrikes
      .filter((s) => s !== snapshot.atmStrike)
      .forEach((s) => drawLevel(s, COLORS.wing, `${s} sold`));
    drawLevel(snapshot.atmStrike, COLORS.atm, `${snapshot.atmStrike} ATM`, '6 3');
    snapshot.breakevens.forEach((be) => drawLevel(be, COLORS.breakeven, `${be} BE`));

    // Spot path
    const line = d3
      .line<{ tick: number; spot: number }>()
      .x((p) => xScale(p.tick))
      .y((p) => yScale(p.spot));
    g.append('path')
      .datum(points)
      .attr('fill', 'none')
      .attr('stroke', COLORS.line)
      .attr('stroke-width', 1.6)
      .attr('d', line);

    // Spike markers
    snapshot.spikes.forEach((sp) => {
      g.append('circle')
        .attr('cx', xScale(sp.tickIndex))
        .attr('cy', yScale(sp.toSpot))
        .attr('r', 5)
        .attr('fill', 'none')
        .attr('stroke', COLORS.spike)
        .attr('stroke-width', 1.5);
    });

    // Current spot pill
    g.append('circle')
      .attr('cx', xScale(points[points.length - 1].tick))
      .attr('cy', yScale(points[points.length - 1].spot))
      .attr('r', 3)
      .attr('fill', COLORS.line);
  }, [snapshot, height]);

  return (
    <Box ref={containerRef} sx={{ width: '100%' }}>
      <Box sx={{ display: 'flex', gap: 2, mb: 0.5, px: 1 }}>
        <Typography variant="caption">
          NIFTY <span style={{ color: '#d1d4dc', fontWeight: 600 }}>{snapshot.spot.toFixed(2)}</span>
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.wing }}>
          — sold strikes
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.atm }}>
          — ATM
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.breakeven }}>
          — breakevens
        </Typography>
        <Typography variant="caption" sx={{ color: COLORS.spike }}>
          ○ spikes
        </Typography>
        <Tooltip title="Sudden 1–3% event-day moves injected into the simulation. IV expands on a spike and partly reverts afterwards." arrow>
          <Typography variant="caption" sx={{ cursor: 'help', textDecoration: 'underline dotted' }}>
            what is a spike?
          </Typography>
        </Tooltip>
      </Box>
      <svg ref={svgRef} />
    </Box>
  );
};
