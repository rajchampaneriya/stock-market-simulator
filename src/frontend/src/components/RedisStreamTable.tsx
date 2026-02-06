import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Box,
  Typography,
  Collapse,
  IconButton,
  Alert,
} from '@mui/material';
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
import type { RedisStreamInfo } from '../types';
import { useState } from 'react';

interface RedisStreamTableProps {
  streams: RedisStreamInfo[];
}

/**
 * Row component with collapsible consumer group details — dark theme
 */
function StreamRow({ stream }: { stream: RedisStreamInfo }) {
  const [open, setOpen] = useState(false);

  const formatTimestamp = (timestamp: string | null) => {
    if (!timestamp) return 'N/A';
    const date = new Date(timestamp);
    return date.toLocaleString();
  };

  const formatNumber = (num: number) => {
    return num.toLocaleString();
  };

  return (
    <>
      <TableRow sx={{ '& > *': { borderBottom: 'unset' } }}>
        <TableCell sx={{ width: 40 }}>
          <IconButton
            aria-label="expand row"
            size="small"
            onClick={() => setOpen(!open)}
            disabled={stream.consumerGroups.length === 0}
            sx={{ color: '#787b86' }}
          >
            {open ? <KeyboardArrowUpIcon fontSize="small" /> : <KeyboardArrowDownIcon fontSize="small" />}
          </IconButton>
        </TableCell>
        <TableCell component="th" scope="row">
          <Typography
            sx={{
              fontSize: '0.8125rem',
              fontWeight: 500,
              fontFamily: "'JetBrains Mono', monospace",
              color: '#d1d4dc',
            }}
          >
            {stream.streamName}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Chip
            label={formatNumber(stream.messageCount)}
            size="small"
            sx={{
              bgcolor: stream.messageCount > 0 ? 'rgba(38, 166, 154, 0.12)' : 'rgba(120, 123, 134, 0.08)',
              color: stream.messageCount > 0 ? '#26a69a' : '#787b86',
              fontFamily: "'JetBrains Mono', monospace",
              fontWeight: 600,
              fontSize: '0.6875rem',
            }}
          />
        </TableCell>
        <TableCell>
          <Typography sx={{ fontSize: '0.75rem', fontFamily: "'JetBrains Mono', monospace", color: '#787b86' }}>
            {formatTimestamp(stream.latestMessageTimestamp)}
          </Typography>
        </TableCell>
        <TableCell>
          <Chip
            label={`${stream.consumerGroups.length} group(s)`}
            size="small"
            variant="outlined"
            sx={{
              borderColor: '#363a45',
              color: '#787b86',
              fontSize: '0.6875rem',
            }}
          />
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={5}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ m: 2 }}>
              <Typography variant="subtitle2" gutterBottom sx={{ color: '#787b86' }}>
                CONSUMER GROUPS
              </Typography>
              {stream.consumerGroups.length === 0 ? (
                <Alert
                  severity="info"
                  sx={{
                    my: 1,
                    bgcolor: 'rgba(41, 98, 255, 0.06)',
                    border: '1px solid rgba(41, 98, 255, 0.15)',
                    color: '#5b8def',
                  }}
                >
                  No consumer groups configured for this stream
                </Alert>
              ) : (
                <Table size="small" aria-label="consumer groups" sx={{ '& .MuiTableCell-root': { borderColor: '#2a2e39' } }}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Group Name</TableCell>
                      <TableCell align="right">Consumers</TableCell>
                      <TableCell align="right">Pending</TableCell>
                      <TableCell>Last Delivered ID</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {stream.consumerGroups.map((group) => (
                      <TableRow key={group.groupName}>
                        <TableCell component="th" scope="row">
                          <Typography sx={{ fontSize: '0.75rem', fontFamily: "'JetBrains Mono', monospace", color: '#d1d4dc' }}>
                            {group.groupName}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography sx={{ fontSize: '0.75rem', fontFamily: "'JetBrains Mono', monospace", color: '#d1d4dc' }}>
                            {group.consumersCount}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Chip
                            label={group.pendingCount}
                            size="small"
                            sx={{
                              bgcolor: group.pendingCount > 0 ? 'rgba(255, 152, 0, 0.12)' : 'rgba(38, 166, 154, 0.12)',
                              color: group.pendingCount > 0 ? '#ff9800' : '#26a69a',
                              fontFamily: "'JetBrains Mono', monospace",
                              fontWeight: 600,
                              fontSize: '0.6875rem',
                            }}
                          />
                        </TableCell>
                        <TableCell>
                          <Typography sx={{ fontSize: '0.6875rem', fontFamily: "'JetBrains Mono', monospace", color: '#5d606b' }}>
                            {group.lastDeliveredId || 'N/A'}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
}

/**
 * Table component displaying Redis stream information — dark terminal style
 */
export function RedisStreamTable({ streams }: RedisStreamTableProps) {
  if (streams.length === 0) {
    return (
      <Alert
        severity="info"
        sx={{
          bgcolor: 'rgba(41, 98, 255, 0.06)',
          border: '1px solid rgba(41, 98, 255, 0.15)',
          color: '#5b8def',
        }}
      >
        No Redis streams found. The Market Simulator may not have started publishing data yet.
      </Alert>
    );
  }

  return (
    <TableContainer
      sx={{
        bgcolor: '#1e222d',
        border: '1px solid #2a2e39',
        borderRadius: 1,
      }}
    >
      <Table aria-label="redis streams table">
        <TableHead>
          <TableRow>
            <TableCell sx={{ width: 40 }} />
            <TableCell>Stream Name</TableCell>
            <TableCell align="right">Messages</TableCell>
            <TableCell>Latest Message</TableCell>
            <TableCell>Consumer Groups</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {streams.map((stream) => (
            <StreamRow key={stream.streamName} stream={stream} />
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
