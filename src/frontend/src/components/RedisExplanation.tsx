import {
  Box,
  Typography,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import InfoIcon from '@mui/icons-material/Info';

/**
 * Educational component explaining Redis Streams — dark theme
 */
export function RedisExplanation() {
  const listItemSx = {
    '& .MuiListItemText-primary': {
      fontSize: '0.8125rem',
      fontWeight: 600,
      color: '#d1d4dc',
    },
    '& .MuiListItemText-secondary': {
      fontSize: '0.75rem',
      color: '#787b86',
    },
  };

  return (
    <Box
      sx={{
        p: 2.5,
        mb: 3,
        bgcolor: '#1e222d',
        border: '1px solid #2a2e39',
        borderRadius: 1,
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <InfoIcon sx={{ mr: 1, color: '#2962ff', fontSize: 20 }} />
        <Typography variant="h3" sx={{ fontSize: '0.9375rem' }}>
          About Redis Streams
        </Typography>
      </Box>

      <Typography sx={{ fontSize: '0.8125rem', color: '#787b86', mb: 2 }}>
        Redis Streams is an append-only log data structure designed for real-time data streaming,
        similar to Apache Kafka topics but with simpler setup and operation.
      </Typography>

      <Accordion defaultExpanded>
        <AccordionSummary expandIcon={<ExpandMoreIcon sx={{ color: '#787b86' }} />}>
          <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: '#d1d4dc' }}>
            How Messages are Appended
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense disablePadding>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Unique IDs"
                secondary="Each message receives a unique ID in the format: timestamp-sequence (e.g., 1234567890123-0)"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Immutable"
                secondary="Once added, messages cannot be modified"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Ordered"
                secondary="Messages are automatically ordered by insertion time"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Trimming"
                secondary="Streams can be automatically trimmed to limit memory usage (e.g., keep only last 10,000 messages)"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>

      <Accordion>
        <AccordionSummary expandIcon={<ExpandMoreIcon sx={{ color: '#787b86' }} />}>
          <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: '#d1d4dc' }}>
            Consumer Groups
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense disablePadding>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Multiple Consumers"
                secondary="Allow multiple consumers to read from the same stream independently"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Position Tracking"
                secondary="Each consumer tracks its own position in the stream"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Acknowledgment"
                secondary="Consumers acknowledge messages (XACK) to mark them as processed"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Horizontal Scaling"
                secondary="Multiple consumer instances can process messages in parallel"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>

      <Accordion>
        <AccordionSummary expandIcon={<ExpandMoreIcon sx={{ color: '#787b86' }} />}>
          <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: '#d1d4dc' }}>
            How This Application Uses Streams
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense disablePadding>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Stream Name"
                secondary={<><code style={{ color: '#26a69a', fontFamily: "'JetBrains Mono', monospace", fontSize: '0.6875rem' }}>marketdata:stream</code> — Contains all market price updates</>}
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Publisher"
                secondary="Market Simulator publishes price ticks via Market Ingestion API"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Consumer"
                secondary={<>Broker Service consumes from the stream using consumer group <code style={{ color: '#26a69a', fontFamily: "'JetBrains Mono', monospace", fontSize: '0.6875rem' }}>brokers</code></>}
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Message Format"
                secondary="Each message contains: symbol, price, volume, and timestamp"
              />
            </ListItem>
            <ListItem sx={listItemSx}>
              <ListItemText
                primary="Retention"
                secondary="Stream is automatically trimmed to retain only the last 10,000 messages"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>
    </Box>
  );
}
