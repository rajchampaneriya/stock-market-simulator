import { Alert, Card, CardContent, LinearProgress, Stack, Typography, Box, Tooltip } from '@mui/material';
import type { Totals } from '../../types/options';

const money = (n: number) => '₹' + n.toLocaleString('en-IN', { maximumFractionDigits: 0 });

export function CapitalCostsPanel({ totals }: { totals: Totals }) {
  const costRows: Array<[string, number]> = [
    ['Brokerage', totals.brokerage], ['STT', totals.stt],
    ['Exchange fees', totals.exchange], ['SEBI fees', totals.sebi],
    ['Stamp duty', totals.stamp], ['GST', totals.gst],
    ['Execution slippage', totals.slippage],
  ];
  return <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mt: 2 }}>
    <Card sx={{ flex: 1 }}><CardContent>
      <Typography variant="h6">Capital & estimated margin</Typography>
      <Typography variant="h5" sx={{ mt: 1 }}>{money(totals.marginRequired)}</Typography>
      <Typography variant="body2" color="text.secondary">Estimated required margin</Typography>
      <LinearProgress variant="determinate" color={totals.marginShortfall ? 'error' : 'primary'}
        value={Math.min(100, Math.max(0, totals.marginUtilization * 100))} sx={{ my: 1.5, height: 9, borderRadius: 1 }} />
      <Stack direction="row" justifyContent="space-between">
        <Typography variant="body2">Utilization {(totals.marginUtilization * 100).toFixed(1)}%</Typography>
        <Typography variant="body2">Available {money(totals.marginAvailable)}</Typography>
      </Stack>
      <Typography variant="body2" sx={{ mt: 1 }}>Adverse scenario loss: {money(totals.marginStressLoss)}</Typography>
      {totals.marginShortfall && <Alert severity="error" sx={{ mt: 1 }}>Estimated margin shortfall. Consider reducing exposure.</Alert>}
      <Tooltip title={totals.marginMethod}><Typography variant="caption" color="text.secondary">
        Educational stress proxy only. Not an exchange SPAN or broker basket-margin quote.
      </Typography></Tooltip>
    </CardContent></Card>
    <Card sx={{ flex: 1 }}><CardContent>
      <Typography variant="h6">Net P/L & trading costs</Typography>
      <Stack direction="row" justifyContent="space-between" sx={{ mt: 1 }}>
        <Typography>Mid-price gross P/L</Typography><Typography>{money(totals.gross)}</Typography>
      </Stack>
      <Stack direction="row" justifyContent="space-between">
        <Typography>Accumulated costs</Typography><Typography>{money(totals.transactionCosts)}</Typography>
      </Stack>
      <Stack direction="row" justifyContent="space-between">
        <Typography fontWeight={700}>Net P/L</Typography>
        <Typography fontWeight={700} color={totals.net >= 0 ? 'success.main' : 'error.main'}>{money(totals.net)}</Typography>
      </Stack>
      <Box sx={{ mt: 1, borderTop: '1px solid #2a2e39', pt: 1 }}>
        {costRows.map(([label, value]) => <Stack key={label} direction="row" justifyContent="space-between">
          <Typography variant="caption">{label}</Typography><Typography variant="caption">{money(value)}</Typography>
        </Stack>)}
      </Box>
      <Typography variant="caption" color="text.secondary">
        Slippage is embedded in executed fill prices and is not deducted twice.
      </Typography>
    </CardContent></Card>
  </Stack>;
}
