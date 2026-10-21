import { useEffect, useState } from 'react';
import { Alert, Button, Card, CardContent, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';

type Risk = { mean: number; median: number; probabilityOfLoss: number; var95: number; expectedShortfall95: number; p05: number; p95: number };
type Result = { hold: Risk; act: Risk; estimatedActionCosts: number; paths: number; seed: number; disclaimer: string };
const inr = (n: number) => '₹' + n.toLocaleString('en-IN', { maximumFractionDigits: 0 });

export function MonteCarloPanel({ brokerUrl, sessionId, tickIndex }: {
  brokerUrl: string; sessionId: string | null; tickIndex: number;
}) {
  const [paths, setPaths] = useState(5000);
  const [seed, setSeed] = useState(42);
  const [hedge, setHedge] = useState(1);
  const [horizon, setHorizon] = useState(30);
  const [result, setResult] = useState<Result | null>(null);
  const [resultTick, setResultTick] = useState<number | null>(null);
  useEffect(() => { setResult(null); setResultTick(null); }, [sessionId]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const project = async () => {
    if (!sessionId) return;
    setLoading(true);
    setError('');
    try {
      const response = await fetch(`${brokerUrl}/api/options/sessions/${sessionId}/monte-carlo`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ signedFuturesHedgeLots: hedge, paths, seed, steps: 30, horizonTradingMinutes: horizon }),
      });
      if (!response.ok) throw new Error(`Simulation returned HTTP ${response.status}`);
      setResult(await response.json() as Result);
      setResultTick(tickIndex);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Projection failed');
    } finally {
      setLoading(false);
    }
  };

  const rows: Array<[string, keyof Risk, boolean]> = [
    ['Mean P/L', 'mean', false], ['Median P/L', 'median', false],
    ['Probability of loss', 'probabilityOfLoss', true], ['95% VaR', 'var95', false],
    ['95% expected shortfall', 'expectedShortfall95', false],
    ['5th percentile', 'p05', false], ['95th percentile', 'p95', false],
  ];
  const format = (value: number, percentage: boolean) =>
    percentage ? (value * 100).toFixed(1) + '%' : inr(value);

  return <Card sx={{ mt: 2 }}><CardContent>
    <Typography variant="h6" gutterBottom>Monte Carlo: hold vs hedge</Typography>
    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
      Paired, seeded scenarios from the current portfolio. Run again after a market move.
    </Typography>
    <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
      <TextField label="Paths" size="small" type="number" value={paths} onChange={e => setPaths(Number(e.target.value))} sx={{ width: 100 }} />
      <TextField label="Seed" size="small" type="number" value={seed} onChange={e => setSeed(Number(e.target.value))} sx={{ width: 100 }} />
      <TextField label="Horizon (min)" size="small" type="number" value={horizon} onChange={e => setHorizon(Number(e.target.value))} sx={{ width: 130 }} />
      <TextField label="Futures lots (+buy / -sell)" size="small" type="number" value={hedge} onChange={e => setHedge(Number(e.target.value))} sx={{ width: 220 }} />
      <Button variant="contained" disabled={!sessionId || loading || paths < 100 || paths > 10000 || horizon <= 0} onClick={project}>
        {loading ? 'Calculating…' : 'Run projection'}
      </Button>
    </Stack>
    {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
    {result && <>
      {resultTick !== tickIndex && <Alert severity="info" sx={{ mt: 2 }}>Market moved since tick {resultTick}; rerun to refresh the projection.</Alert>}
      <Table size="small" sx={{ mt: 2 }}>
        <TableHead><TableRow><TableCell>Risk measure</TableCell><TableCell align="right">Hold</TableCell><TableCell align="right">Hedge</TableCell></TableRow></TableHead>
        <TableBody>{rows.map(([label, key, percentage]) =>
          <TableRow key={key}><TableCell>{label}</TableCell>
            <TableCell align="right">{format(result.hold[key], percentage)}</TableCell>
            <TableCell align="right">{format(result.act[key], percentage)}</TableCell>
          </TableRow>)}</TableBody>
      </Table>
      <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
        Estimated opening hedge cost: {inr(result.estimatedActionCosts)} · {result.paths.toLocaleString()} paths · seed {result.seed}
      </Typography>
      <Alert severity="warning" sx={{ mt: 1 }}>{result.disclaimer}</Alert>
    </>}
  </CardContent></Card>;
}
