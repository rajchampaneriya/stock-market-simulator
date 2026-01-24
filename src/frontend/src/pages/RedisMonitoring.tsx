import { useState, useEffect, useCallback } from 'react';
import {
  Container,
  Typography,
  Box,
  Button,
  CircularProgress,
  Alert,
  Paper,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { RedisStreamTable } from '../components/RedisStreamTable';
import { RedisExplanation } from '../components/RedisExplanation';
import { RedisMonitoringService } from '../services/redisMonitoringService';
import type { RedisStreamInfo } from '../types';

/**
 * Redis Monitoring Page
 * Displays Redis Streams activity for debugging and learning
 */
export function RedisMonitoring() {
  const [streams, setStreams] = useState<RedisStreamInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  // Use empty string to make requests relative (will use Vite proxy)
  // In production, this would be the actual broker URL from env var
  const brokerUrl = import.meta.env.DEV ? '' : (import.meta.env.VITE_BROKER_URL || 'http://localhost:5002');
  const service = new RedisMonitoringService(brokerUrl);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await service.fetchStreamsInfo();
      setStreams(data);
      setLastUpdated(new Date());
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch stream information';
      setError(errorMessage);
      console.error('Error fetching stream info:', err);
    } finally {
      setLoading(false);
    }
  }, [brokerUrl]);

  // Fetch data on component mount
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleRefresh = () => {
    fetchData();
  };

  return (
    <Container maxWidth="lg">
      <Box sx={{ my: 4 }}>
        {/* Page Header */}
        <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <Typography variant="h4" component="h1" gutterBottom>
              Redis Stream Monitoring
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              View real-time Redis Streams activity and verify data flow
            </Typography>
          </div>
          <Button
            variant="contained"
            startIcon={loading ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
            onClick={handleRefresh}
            disabled={loading}
          >
            Refresh
          </Button>
        </Box>

        {/* Last Updated Info */}
        {lastUpdated && !loading && (
          <Paper elevation={0} sx={{ p: 1.5, mb: 2, backgroundColor: '#f5f5f5' }}>
            <Typography variant="body2" color="text.secondary">
              Last updated: {lastUpdated.toLocaleString()}
            </Typography>
          </Paper>
        )}

        {/* Error Display */}
        {error && (
          <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Loading State */}
        {loading && streams.length === 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', my: 4 }}>
            <CircularProgress sx={{ mr: 2 }} />
            <Typography>Loading stream information...</Typography>
          </Box>
        )}

        {/* Stream Table */}
        {!loading || streams.length > 0 ? (
          <Box sx={{ mb: 4 }}>
            <Typography variant="h5" component="h2" gutterBottom>
              Active Streams
            </Typography>
            <RedisStreamTable streams={streams} />
          </Box>
        ) : null}

        {/* Educational Content */}
        <RedisExplanation />

        {/* Debug Information */}
        <Paper elevation={1} sx={{ p: 2, mt: 3, backgroundColor: '#fafafa' }}>
          <Typography variant="subtitle2" gutterBottom>
            Debug Information
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Broker URL: <code>{brokerUrl}</code>
          </Typography>
          <Typography variant="body2" color="text.secondary">
            API Endpoint: <code>{brokerUrl}/api/redis/streams/info</code>
          </Typography>
        </Paper>
      </Box>
    </Container>
  );
}
