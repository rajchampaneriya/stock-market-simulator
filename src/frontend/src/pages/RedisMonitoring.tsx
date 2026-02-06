import { useState, useEffect, useCallback } from 'react';
import {
  Typography,
  Box,
  Button,
  CircularProgress,
  Alert,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { RedisStreamTable } from '../components/RedisStreamTable';
import { RedisExplanation } from '../components/RedisExplanation';
import { RedisMonitoringService } from '../services/redisMonitoringService';
import type { RedisStreamInfo } from '../types';

/**
 * Redis Monitoring Page — Dark terminal-style
 */
export function RedisMonitoring() {
  const [streams, setStreams] = useState<RedisStreamInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

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

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleRefresh = () => {
    fetchData();
  };

  return (
    <Box sx={{ flex: 1, overflow: 'auto', bgcolor: '#131722' }}>
      <Box sx={{ maxWidth: 1200, mx: 'auto', p: 3 }}>
        {/* Page Header */}
        <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            <Typography variant="h2" sx={{ mb: 0.5 }}>
              Redis Stream Monitoring
            </Typography>
            <Typography variant="subtitle1" sx={{ color: '#787b86' }}>
              Real-time Redis Streams activity and data flow verification
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            {lastUpdated && !loading && (
              <Typography variant="caption" sx={{ color: '#5d606b', fontFamily: "'JetBrains Mono', monospace" }}>
                {lastUpdated.toLocaleTimeString()}
              </Typography>
            )}
            <Button
              variant="outlined"
              size="small"
              startIcon={loading ? <CircularProgress size={14} color="inherit" /> : <RefreshIcon sx={{ fontSize: 16 }} />}
              onClick={handleRefresh}
              disabled={loading}
              sx={{
                borderColor: '#363a45',
                color: '#d1d4dc',
                '&:hover': {
                  borderColor: '#2962ff',
                  bgcolor: 'rgba(41, 98, 255, 0.08)',
                },
              }}
            >
              Refresh
            </Button>
          </Box>
        </Box>

        {/* Error */}
        {error && (
          <Alert
            severity="error"
            sx={{
              mb: 3,
              bgcolor: 'rgba(239, 83, 80, 0.08)',
              border: '1px solid rgba(239, 83, 80, 0.2)',
              color: '#ef5350',
            }}
            onClose={() => setError(null)}
          >
            {error}
          </Alert>
        )}

        {/* Loading */}
        {loading && streams.length === 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', my: 6 }}>
            <CircularProgress size={28} sx={{ mr: 2, color: '#2962ff' }} />
            <Typography sx={{ color: '#787b86', fontSize: '0.8125rem' }}>
              Loading stream information...
            </Typography>
          </Box>
        )}

        {/* Stream Table */}
        {(!loading || streams.length > 0) && (
          <Box sx={{ mb: 4 }}>
            <Typography variant="subtitle2" sx={{ mb: 1.5, color: '#787b86' }}>
              ACTIVE STREAMS
            </Typography>
            <RedisStreamTable streams={streams} />
          </Box>
        )}

        {/* Educational Content */}
        <RedisExplanation />

        {/* Debug bar */}
        <Box
          sx={{
            mt: 3,
            p: 1.5,
            borderRadius: 1,
            bgcolor: '#1e222d',
            border: '1px solid #2a2e39',
            display: 'flex',
            gap: 3,
            alignItems: 'center',
          }}
        >
          <Typography variant="caption" sx={{ color: '#5d606b', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Debug
          </Typography>
          <Typography variant="caption" sx={{ fontFamily: "'JetBrains Mono', monospace", color: '#787b86' }}>
            broker: {brokerUrl || 'proxy'}
          </Typography>
          <Typography variant="caption" sx={{ fontFamily: "'JetBrains Mono', monospace", color: '#787b86' }}>
            endpoint: /api/redis/streams/info
          </Typography>
        </Box>
      </Box>
    </Box>
  );
}
