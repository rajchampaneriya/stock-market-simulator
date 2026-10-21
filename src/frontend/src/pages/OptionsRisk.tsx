import { useState } from 'react';
import {
  Alert, Accordion, AccordionDetails, AccordionSummary, Box, Button, Card, CardContent,
  MenuItem, Stack, TextField, Tooltip, Typography,
} from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import ReplayIcon from '@mui/icons-material/Replay';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { useOptionsRisk } from '../hooks/useOptionsRisk';
import { NiftySpotChart } from '../components/options/NiftySpotChart';
import { PayoffDiagram } from '../components/options/PayoffDiagram';
import { PositionsTable } from '../components/options/PositionsTable';
import { CoachPanel } from '../components/options/CoachPanel';

const fmt = (n: number) => n.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const pnlColor = (n: number) => (n >= 0 ? '#26a69a' : '#ef5350');

const GLOSSARY: Array<[string, string]> = [
  ['Short straddle', 'Selling both the ATM call and the ATM put. You collect the fattest premium but take unlimited risk in either direction.'],
  ['Short strangle', 'Selling an OTM call and an OTM put (here ±500 points). Lower premium than a straddle, but a wider profit zone.'],
  ['Breakeven', "The spot level where the strategy's P/L at expiry is exactly zero. For a short straddle: strike ± net premium collected."],
  ['Theta decay', 'Options lose time value as expiry approaches. Sellers earn theta — it is the income side of the trade.'],
  ['Implied volatility (IV)', "The market's forecast of future movement, embedded in option prices. Short options lose when IV rises."],
  ['Delta', 'P/L sensitivity to a 1-point spot move. Net delta near zero means the book is directionally hedged.'],
  ['Gamma', 'How fast delta changes with spot. Short options are short gamma — adverse moves accelerate against you.'],
  ['Vega', 'P/L sensitivity to a 1 vol-point change in IV. This is what hurts when an event spike expands IV.'],
  ['Cost of carry / futures', 'NIFTY futures trade at spot × e^(rate × time). Futures with delta ≈ 1 per unit are the standard delta hedge.'],
];

export function OptionsRisk() {
  const brokerUrl = import.meta.env.DEV ? '' : (import.meta.env.VITE_BROKER_URL || 'http://localhost:5002');
  const { config, configError, snapshot, connected, start, pause, resume, setSpeed, applySuggestion, ignoreSuggestion } =
    useOptionsRisk(brokerUrl);
  const [seed, setSeed] = useState('42');

  if (configError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Options simulator unavailable — Broker Service not reachable or the feature flag is off.
        </Alert>
      </Box>
    );
  }

  if (!config?.enabled) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="info">
          The Options Selling Risk Simulator is disabled on this deployment
          (Broker config <code>Features:EnableOptionsRisk</code>).
        </Alert>
      </Box>
    );
  }

  const running = snapshot?.status === 'Running';
  const paused = snapshot?.status === 'Paused';
  const finished = snapshot?.status === 'Finished';

  const stat = (label: string, value: string, color?: string, tip?: string) => (
    <Tooltip title={tip ?? ''} arrow disableHoverListener={!tip}>
      <Box sx={{ minWidth: 130 }}>
        <Typography variant="subtitle2">{label}</Typography>
        <Typography variant="h5" sx={{ color: color ?? '#d1d4dc', fontFamily: 'monospace' }}>
          {value}
        </Typography>
      </Box>
    </Tooltip>
  );

  return (
    <Box sx={{ p: 2, maxWidth: 1500, mx: 'auto' }}>
      <Alert severity="warning" sx={{ mb: 2 }}>
        <strong>Educational only, not investment advice.</strong> Simulated markets, Black-Scholes
        pricing, no margin, charges or slippage. No real orders, no broker integration.
      </Alert>

      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="h2">Options Selling Risk Simulator</Typography>
        <Stack direction="row" spacing={1} alignItems="center">
          <TextField
            size="small"
            label="Seed"
            value={seed}
            onChange={(e) => setSeed(e.target.value)}
            sx={{ width: 90 }}
            slotProps={{ htmlInput: { inputMode: 'numeric' } }}
          />
          <Button
            variant="contained"
            startIcon={<ReplayIcon />}
            onClick={() => start(Number(seed) || undefined)}
          >
            {snapshot ? 'Restart run' : 'Open strategy'}
          </Button>
          {running && (
            <Button variant="outlined" startIcon={<PauseIcon />} onClick={pause}>
              Pause
            </Button>
          )}
          {paused && (
            <Button variant="contained" startIcon={<PlayArrowIcon />} onClick={resume}>
              Resume
            </Button>
          )}
          <TextField
            select
            size="small"
            label="Speed"
            value={snapshot?.speed ?? 1}
            onChange={(e) => setSpeed(Number(e.target.value))}
            sx={{ width: 96 }}
            disabled={!snapshot || finished}
          >
            {config.speeds.map((s) => (
              <MenuItem key={s} value={s}>
                {s}×
              </MenuItem>
            ))}
          </TextField>
          <Tooltip title={connected ? 'Streaming live snapshots' : 'SSE disconnected'}>
            <Box
              sx={{
                width: 10, height: 10, borderRadius: '50%',
                bgcolor: connected ? '#26a69a' : '#ef5350',
              }}
            />
          </Tooltip>
        </Stack>
      </Stack>

      {!snapshot && (
        <Card>
          <CardContent>
            <Typography variant="body1">
              Press <strong>Open strategy</strong> to sell 1 lot each of the ATM straddle and the
              ±{fmt(config.wingOffset)}-point strangle around NIFTY {fmt(25000)}. Expiry in{' '}
              {config.daysToExpiry} simulated trading day(s). A seeded run is reproducible; leave the
              seed fixed to practise the same market twice.
            </Typography>
          </CardContent>
        </Card>
      )}

      {snapshot && (
        <>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Stack direction="row" spacing={4} sx={{ flexWrap: 'wrap', gap: 2 }}>
                {stat('NIFTY spot', fmt(snapshot.spot))}
                {stat('Status', snapshot.status)}
                {stat('Time to expiry', `${(snapshot.minutesToExpiry / 375).toFixed(2)} d`,
                  undefined, 'Simulated trading time left until the options expire (375 trading minutes per day).')}
                {stat('IV multiplier', `${snapshot.ivMultiplier.toFixed(2)}×`)}
                {stat('Net P/L', `₹${fmt(snapshot.totals.net)}`, pnlColor(snapshot.totals.net),
                  'Realized + unrealized across every leg and hedge.')}
                {stat('Max loss so far', `₹${fmt(snapshot.totals.maxLossSoFar)}`, '#ef5350')}
                {stat('Premium collected', `₹${fmt(snapshot.totals.premiumCollected)}`, '#26a69a')}
                {stat('Drawdown', `${(snapshot.totals.drawdownFraction * 100).toFixed(1)}%`)}
              </Stack>
            </CardContent>
          </Card>

          <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
            <Card sx={{ flex: 3 }}>
              <CardContent>
                <NiftySpotChart snapshot={snapshot} />
              </CardContent>
            </Card>
            <Card sx={{ flex: 2 }}>
              <CardContent>
                <PayoffDiagram snapshot={snapshot} />
              </CardContent>
            </Card>
          </Stack>

          <Card sx={{ my: 2 }}>
            <PositionsTable snapshot={snapshot} />
          </Card>

          <Card>
            <CardContent>
              <CoachPanel snapshot={snapshot} onApply={applySuggestion} onIgnore={ignoreSuggestion} />
            </CardContent>
          </Card>
        </>
      )}

      <Accordion sx={{ mt: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="h6">Concepts used on this screen</Typography>
        </AccordionSummary>
        <AccordionDetails>
          {GLOSSARY.map(([term, text]) => (
            <Box key={term} sx={{ mb: 1 }}>
              <Typography component="span" sx={{ fontWeight: 700 }}>
                {term}:
              </Typography>{' '}
              <Typography component="span" variant="body2" sx={{ color: '#a7abbb' }}>
                {text}
              </Typography>
            </Box>
          ))}
        </AccordionDetails>
      </Accordion>
    </Box>
  );
}
