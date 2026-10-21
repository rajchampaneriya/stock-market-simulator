import React from 'react';
import {
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Tooltip, Typography,
} from '@mui/material';
import type { OptionsSnapshot } from '../../types/options';

const fmt = (n: number, digits = 2) =>
  n.toLocaleString('en-IN', { minimumFractionDigits: digits, maximumFractionDigits: digits });
const pnlColor = (n: number) => (n > 0 ? '#26a69a' : n < 0 ? '#ef5350' : '#d1d4dc');

const HeaderTip: React.FC<{ title: string; tip: string }> = ({ title, tip }) => (
  <Tooltip title={tip} arrow>
    <span style={{ cursor: 'help', borderBottom: '1px dotted #5d606b' }}>{title}</span>
  </Tooltip>
);

const greekTips = {
  delta: 'Delta: sensitivity of the option price to a 1-point move in NIFTY. A short call has negative delta for you — you lose as spot rises.',
  gamma: 'Gamma: how fast delta changes. Short options = short gamma: moves against you accelerate.',
  theta: 'Theta: daily premium decay in your favour as the seller — the income you keep if nothing moves.',
  vega: 'Vega: P/L per 1 vol-point change in implied volatility. Short options lose when IV spikes.',
} as const;

interface Props {
  snapshot: OptionsSnapshot;
}

/** One row per option leg + futures hedge, plus totals row. */
export const PositionsTable: React.FC<Props> = ({ snapshot }) => (
  <TableContainer>
    <Table size="small" stickyHeader>
      <TableHead>
        <TableRow>
          <TableCell>Instrument</TableCell>
          <TableCell align="right">Side / Qty</TableCell>
          <TableCell align="right">Entry</TableCell>
          <TableCell align="right">LTP</TableCell>
          <TableCell align="right">IV</TableCell>
          <TableCell align="right"><HeaderTip title="Delta" tip={greekTips.delta} /></TableCell>
          <TableCell align="right"><HeaderTip title="Gamma" tip={greekTips.gamma} /></TableCell>
          <TableCell align="right"><HeaderTip title="Theta/d" tip={greekTips.theta} /></TableCell>
          <TableCell align="right"><HeaderTip title="Vega" tip={greekTips.vega} /></TableCell>
          <TableCell align="right">Unrealized</TableCell>
          <TableCell align="right">Realized</TableCell>
          <TableCell align="right">Status</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {snapshot.legs.map((leg) => (
          <TableRow key={leg.id} sx={{ opacity: leg.status === 'Exited' ? 0.5 : 1 }}>
            <TableCell sx={{ fontWeight: 600 }}>{leg.id}</TableCell>
            <TableCell align="right">{leg.side} {leg.lots}L</TableCell>
            <TableCell align="right">{fmt(leg.entryPrice, 1)}</TableCell>
            <TableCell align="right">{fmt(leg.lastPrice, 1)}</TableCell>
            <TableCell align="right">{(leg.iv * 100).toFixed(1)}%</TableCell>
            <TableCell align="right">{leg.delta.toFixed(3)}</TableCell>
            <TableCell align="right">{leg.gamma.toFixed(4)}</TableCell>
            <TableCell align="right" sx={{ color: '#26a69a' }}>{fmt(leg.thetaPerDay, 0)}</TableCell>
            <TableCell align="right">{fmt(leg.vegaPerPoint, 0)}</TableCell>
            <TableCell align="right" sx={{ color: pnlColor(leg.unrealized) }}>{fmt(leg.unrealized, 0)}</TableCell>
            <TableCell align="right" sx={{ color: pnlColor(leg.realized) }}>{fmt(leg.realized, 0)}</TableCell>
            <TableCell align="right">{leg.status}</TableCell>
          </TableRow>
        ))}
        {snapshot.futures.map((f) => (
          <TableRow key={f.id}>
            <TableCell sx={{ fontWeight: 600 }}>NIFTY FUT</TableCell>
            <TableCell align="right">{f.side} {f.lots}L</TableCell>
            <TableCell align="right">{fmt(f.entryPrice, 1)}</TableCell>
            <TableCell align="right">{fmt(f.lastPrice, 1)}</TableCell>
            <TableCell align="right">—</TableCell>
            <TableCell align="right">{f.side === 'LONG' ? f.lots * snapshot.lotSize : -f.lots * snapshot.lotSize}</TableCell>
            <TableCell align="right">—</TableCell>
            <TableCell align="right">—</TableCell>
            <TableCell align="right">—</TableCell>
            <TableCell align="right" sx={{ color: pnlColor(f.unrealized) }}>{fmt(f.unrealized, 0)}</TableCell>
            <TableCell align="right" sx={{ color: pnlColor(f.realized) }}>{fmt(f.realized, 0)}</TableCell>
            <TableCell align="right">{f.status}</TableCell>
          </TableRow>
        ))}
        <TableRow sx={{ backgroundColor: '#2a2e39', '& td': { fontWeight: 700 } }}>
          <TableCell>TOTALS</TableCell>
          <TableCell align="right">
            <Tooltip title="Net portfolio greeks: sum of every leg's exposure including futures hedges." arrow>
              <span>
                Δ {snapshot.totals.netDelta.toFixed(0)} · Γ {snapshot.totals.netGamma.toFixed(3)} · Θ{' '}
                {fmt(snapshot.totals.netThetaPerDay, 0)} · V {fmt(snapshot.totals.netVegaPerPoint, 0)}
              </span>
            </Tooltip>
          </TableCell>
          <TableCell align="right" colSpan={2}>
            Premium {fmt(snapshot.totals.premiumCollected, 0)}
          </TableCell>
          <TableCell align="right" colSpan={2}>
            Max loss {fmt(snapshot.totals.maxLossSoFar, 0)}
          </TableCell>
          <TableCell align="right" colSpan={2}>
            <Tooltip title="Peak-to-trough drawdown as a share of virtual capital." arrow>
              <span>DD {(snapshot.totals.drawdownFraction * 100).toFixed(1)}%</span>
            </Tooltip>
          </TableCell>
          <TableCell align="right" sx={{ color: pnlColor(snapshot.totals.unrealized) }}>
            {fmt(snapshot.totals.unrealized, 0)}
          </TableCell>
          <TableCell align="right" sx={{ color: pnlColor(snapshot.totals.realized) }}>
            {fmt(snapshot.totals.realized, 0)}
          </TableCell>
          <TableCell align="right" sx={{ color: pnlColor(snapshot.totals.net) }}>
            <Tooltip title="Net P/L = realized + unrealized across all legs and hedges." arrow>
              <span>{fmt(snapshot.totals.net, 0)}</span>
            </Tooltip>
          </TableCell>
        </TableRow>
      </TableBody>
    </Table>
    <Typography variant="caption" sx={{ display: 'block', p: 1 }}>
      Premium collected is the credit received for the shorts. Max loss is the worst net P/L touched
      during the run — for an unhedged short straddle it can grow without limit.
    </Typography>
  </TableContainer>
);
