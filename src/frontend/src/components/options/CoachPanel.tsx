import React from 'react';
import {
  Alert, Box, Button, Chip, Divider, List, ListItem, Stack, Tooltip, Typography,
} from '@mui/material';
import type { CoachSuggestion, Decision, OptionsSnapshot } from '../../types/options';

const fmt = (n: number) => n.toLocaleString('en-IN', { maximumFractionDigits: 0 });

const ruleSeverity = (rule: string): 'warning' | 'error' | 'info' => {
  if (rule.includes('Drawdown')) return 'error';
  if (rule.includes('IV')) return 'warning';
  return 'info';
};

const outcomeColor = (outcome: string) =>
  outcome === 'applied' ? '#26a69a' : outcome === 'ignored' ? '#ef5350' : '#787b86';

interface SuggestionCardProps {
  suggestion: CoachSuggestion;
  onApply: (id: string) => void;
  onIgnore: (id: string) => void;
}

const SuggestionCard: React.FC<SuggestionCardProps> = ({ suggestion, onApply, onIgnore }) => (
  <ListItem sx={{ display: 'block', px: 0 }}>
    <Alert severity={ruleSeverity(suggestion.rule)} variant="outlined" sx={{ '& .MuiAlert-message': { width: '100%' } }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
        <Chip label={suggestion.rule} size="small" />
        <Typography variant="subtitle1" sx={{ fontWeight: 700, color: '#d1d4dc' }}>
          {suggestion.title}
        </Typography>
      </Stack>
      <Typography variant="body2" sx={{ color: '#a7abbb', whiteSpace: 'normal' }}>
        {suggestion.explanation}
      </Typography>
      <Stack direction="row" spacing={3} sx={{ mt: 1 }}>
        <Tooltip title={`Estimated P/L over the next short horizon if a ${fmt(suggestion.projection.shockMove)}-point adverse move hits and you DO NOT act.`} arrow>
          <Typography variant="body2">
            Hold: <span style={{ color: '#ef5350', fontWeight: 600 }}>₹{fmt(suggestion.projection.holdPnl)}</span>
          </Typography>
        </Tooltip>
        <Tooltip title="Same adverse move, but with the suggestion applied first." arrow>
          <Typography variant="body2">
            Act: <span style={{ color: '#26a69a', fontWeight: 600 }}>₹{fmt(suggestion.projection.actPnl)}</span>
          </Typography>
        </Tooltip>
      </Stack>
      <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
        <Button size="small" variant="contained" color="primary" onClick={() => onApply(suggestion.id)}>
          Apply
        </Button>
        <Button size="small" variant="outlined" onClick={() => onIgnore(suggestion.id)}>
          Ignore
        </Button>
      </Stack>
    </Alert>
  </ListItem>
);

interface Props {
  snapshot: OptionsSnapshot;
  onApply: (id: string) => void;
  onIgnore: (id: string) => void;
}

/** Risk coach: current suggestions + decision/event log with later outcomes. */
export const CoachPanel: React.FC<Props> = ({ snapshot, onApply, onIgnore }) => {
  const decisions = [...snapshot.decisions].reverse();

  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 1 }}>
        Risk Coach — what's the next move?
      </Typography>

      {snapshot.suggestions.length === 0 ? (
        <Alert severity="success" variant="outlined">
          No action needed. The book is inside every risk threshold — let theta work.
        </Alert>
      ) : (
        <List dense disablePadding>
          {snapshot.suggestions.map((s) => (
            <SuggestionCard key={s.id} suggestion={s} onApply={onApply} onIgnore={onIgnore} />
          ))}
        </List>
      )}

      <Divider sx={{ my: 2 }} />
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        Decision &amp; event log
      </Typography>
      <Box sx={{ maxHeight: 260, overflowY: 'auto' }}>
        {decisions.length === 0 && <Typography variant="body2">No decisions yet.</Typography>}
        {decisions.map((d: Decision, i) => (
          <Box key={`${d.tickIndex}-${i}`} sx={{ py: 0.5, borderBottom: '1px solid #2a2e39' }}>
            <Stack direction="row" spacing={1} alignItems="baseline">
              <Typography variant="caption" sx={{ fontFamily: 'monospace', color: '#5d606b' }}>
                t={d.tickIndex}
              </Typography>
              <Chip
                label={d.outcome}
                size="small"
                sx={{ color: outcomeColor(d.outcome), borderColor: outcomeColor(d.outcome) }}
                variant="outlined"
              />
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {d.rule}
              </Typography>
              <Typography variant="body2" sx={{ color: '#787b86' }}>
                {d.description}
              </Typography>
              {d.outcomePnl != null && d.outcome !== 'opened' && (
                <Tooltip title="Net P/L change since this decision was taken." arrow>
                  <Typography variant="body2" sx={{ color: d.outcomePnl >= 0 ? '#26a69a' : '#ef5350', fontWeight: 600 }}>
                    since: ₹{fmt(d.outcomePnl)}
                  </Typography>
                </Tooltip>
              )}
            </Stack>
          </Box>
        ))}
      </Box>
    </Box>
  );
};
