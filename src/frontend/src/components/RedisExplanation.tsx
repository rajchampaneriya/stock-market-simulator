import {
  Box,
  Typography,
  Paper,
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
 * Educational component explaining Redis Streams
 */
export function RedisExplanation() {
  return (
    <Paper elevation={2} sx={{ p: 3, mb: 3, backgroundColor: '#f5f5f5' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <InfoIcon sx={{ mr: 1, color: 'primary.main' }} />
        <Typography variant="h6" component="h2">
          About Redis Streams
        </Typography>
      </Box>

      <Typography variant="body1" paragraph>
        Redis Streams is an append-only log data structure designed for real-time data streaming, 
        similar to Apache Kafka topics but with simpler setup and operation.
      </Typography>

      <Accordion defaultExpanded>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" fontWeight="medium">
            How Messages are Appended
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense>
            <ListItem>
              <ListItemText 
                primary="Unique IDs" 
                secondary="Each message receives a unique ID in the format: timestamp-sequence (e.g., 1234567890123-0)"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Immutable" 
                secondary="Once added, messages cannot be modified"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Ordered" 
                secondary="Messages are automatically ordered by insertion time"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Trimming" 
                secondary="Streams can be automatically trimmed to limit memory usage (e.g., keep only last 10,000 messages)"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>

      <Accordion>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" fontWeight="medium">
            Consumer Groups
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense>
            <ListItem>
              <ListItemText 
                primary="Multiple Consumers" 
                secondary="Allow multiple consumers to read from the same stream independently"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Position Tracking" 
                secondary="Each consumer tracks its own position in the stream"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Acknowledgment" 
                secondary="Consumers acknowledge messages (XACK) to mark them as processed"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Horizontal Scaling" 
                secondary="Multiple consumer instances can process messages in parallel"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>

      <Accordion>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" fontWeight="medium">
            How This Application Uses Streams
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List dense>
            <ListItem>
              <ListItemText 
                primary="Stream Name" 
                secondary="marketdata:stream - Contains all market price updates"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Publisher" 
                secondary="Market Simulator publishes price ticks via Market Ingestion API"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Consumer" 
                secondary="Broker Service consumes from the stream using consumer group 'brokers'"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Message Format" 
                secondary="Each message contains: symbol, price, volume, and timestamp"
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Retention" 
                secondary="Stream is automatically trimmed to retain only the last 10,000 messages"
              />
            </ListItem>
          </List>
        </AccordionDetails>
      </Accordion>
    </Paper>
  );
}
