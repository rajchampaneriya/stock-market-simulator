import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
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
 * Row component with collapsible consumer group details
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
        <TableCell>
          <IconButton
            aria-label="expand row"
            size="small"
            onClick={() => setOpen(!open)}
            disabled={stream.consumerGroups.length === 0}
          >
            {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
          </IconButton>
        </TableCell>
        <TableCell component="th" scope="row">
          <Typography variant="body2" fontWeight="medium" fontFamily="monospace">
            {stream.streamName}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Chip 
            label={formatNumber(stream.messageCount)} 
            color={stream.messageCount > 0 ? 'success' : 'default'}
            size="small"
          />
        </TableCell>
        <TableCell>
          <Typography variant="body2">
            {formatTimestamp(stream.latestMessageTimestamp)}
          </Typography>
        </TableCell>
        <TableCell>
          <Chip 
            label={`${stream.consumerGroups.length} group(s)`}
            size="small"
            variant="outlined"
          />
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={5}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ margin: 2 }}>
              <Typography variant="subtitle2" gutterBottom component="div">
                Consumer Groups
              </Typography>
              {stream.consumerGroups.length === 0 ? (
                <Alert severity="info" sx={{ my: 1 }}>
                  No consumer groups configured for this stream
                </Alert>
              ) : (
                <Table size="small" aria-label="consumer groups">
                  <TableHead>
                    <TableRow>
                      <TableCell>Group Name</TableCell>
                      <TableCell align="right">Consumers</TableCell>
                      <TableCell align="right">Pending Messages</TableCell>
                      <TableCell>Last Delivered ID</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {stream.consumerGroups.map((group) => (
                      <TableRow key={group.groupName}>
                        <TableCell component="th" scope="row">
                          <Typography variant="body2" fontFamily="monospace">
                            {group.groupName}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">{group.consumersCount}</TableCell>
                        <TableCell align="right">
                          <Chip 
                            label={group.pendingCount}
                            color={group.pendingCount > 0 ? 'warning' : 'success'}
                            size="small"
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace" fontSize="0.75rem">
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
 * Table component displaying Redis stream information
 */
export function RedisStreamTable({ streams }: RedisStreamTableProps) {
  if (streams.length === 0) {
    return (
      <Alert severity="info">
        No Redis streams found. The Market Simulator may not have started publishing data yet.
      </Alert>
    );
  }

  return (
    <TableContainer component={Paper}>
      <Table aria-label="redis streams table">
        <TableHead>
          <TableRow>
            <TableCell />
            <TableCell>Stream Name</TableCell>
            <TableCell align="right">Message Count</TableCell>
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
