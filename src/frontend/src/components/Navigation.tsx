import { AppBar, Toolbar, Typography, Button, Box, Tooltip } from '@mui/material';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import ShowChartIcon from '@mui/icons-material/ShowChart';
import StorageIcon from '@mui/icons-material/Storage';
import PieChartIcon from '@mui/icons-material/PieChart';
import { useState, useEffect } from 'react';

/**
 * TradingView-inspired navigation header
 */
export function Navigation() {
  const location = useLocation();
  const [clock, setClock] = useState(new Date());

  useEffect(() => {
    const timer = setInterval(() => setClock(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const formatTime = (d: Date) =>
    d.toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' });
  const formatDate = (d: Date) =>
    d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  return (
    <AppBar position="sticky" elevation={0}>
      <Toolbar sx={{ gap: 1, justifyContent: 'space-between' }}>
        {/* Left: Logo + Nav */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          {/* Logo area */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mr: 1 }}>
            <Box
              sx={{
                width: 28,
                height: 28,
                borderRadius: '6px',
                background: 'linear-gradient(135deg, #2962ff 0%, #7c4dff 100%)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ShowChartIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Box>
            <Typography
              variant="h6"
              component="div"
              sx={{
                fontWeight: 700,
                fontSize: '0.9375rem',
                color: '#d1d4dc',
                letterSpacing: '-0.01em',
              }}
            >
              StockSim
            </Typography>
          </Box>

          {/* Divider */}
          <Box sx={{ width: '1px', height: 24, bgcolor: 'divider' }} />

          {/* Nav tabs */}
          <Box sx={{ display: 'flex', gap: 0.5 }}>
            <Button
              component={RouterLink}
              to="/"
              size="small"
              sx={{
                color: location.pathname === '/' ? '#fff' : '#787b86',
                bgcolor: location.pathname === '/' ? 'rgba(41, 98, 255, 0.15)' : 'transparent',
                borderRadius: '4px',
                px: 1.5,
                py: 0.5,
                minHeight: 32,
                fontSize: '0.8125rem',
                fontWeight: location.pathname === '/' ? 600 : 400,
                '&:hover': {
                  bgcolor: location.pathname === '/' ? 'rgba(41, 98, 255, 0.2)' : 'rgba(255,255,255,0.04)',
                  color: '#d1d4dc',
                },
              }}
            >
              Market
            </Button>
            <Button
              component={RouterLink}
              to="/redis-monitoring"
              size="small"
              startIcon={<StorageIcon sx={{ fontSize: '14px !important' }} />}
              sx={{
                color: location.pathname === '/redis-monitoring' ? '#fff' : '#787b86',
                bgcolor: location.pathname === '/redis-monitoring' ? 'rgba(41, 98, 255, 0.15)' : 'transparent',
                borderRadius: '4px',
                px: 1.5,
                py: 0.5,
                minHeight: 32,
                fontSize: '0.8125rem',
                fontWeight: location.pathname === '/redis-monitoring' ? 600 : 400,
                '&:hover': {
                  bgcolor: location.pathname === '/redis-monitoring' ? 'rgba(41, 98, 255, 0.2)' : 'rgba(255,255,255,0.04)',
                  color: '#d1d4dc',
                },
              }}
            >
              Streams
            </Button>
            <Button
              component={RouterLink}
              to="/options"
              size="small"
              startIcon={<PieChartIcon sx={{ fontSize: '14px !important' }} />}
              sx={{
                color: location.pathname === '/options' ? '#fff' : '#787b86',
                bgcolor: location.pathname === '/options' ? 'rgba(41, 98, 255, 0.15)' : 'transparent',
                borderRadius: '4px',
                px: 1.5,
                py: 0.5,
                minHeight: 32,
                fontSize: '0.8125rem',
                fontWeight: location.pathname === '/options' ? 600 : 400,
                '&:hover': {
                  bgcolor: location.pathname === '/options' ? 'rgba(41, 98, 255, 0.2)' : 'rgba(255,255,255,0.04)',
                  color: '#d1d4dc',
                },
              }}
            >
              Options Lab
            </Button>
          </Box>
        </Box>

        {/* Right: Architecture badge + Clock */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Tooltip title=".NET Aspire orchestration with Redis Streams pub/sub and Server-Sent Events" arrow>
            <Box className="arch-badge">
              Aspire &bull; Redis Streams &bull; SSE
            </Box>
          </Tooltip>

          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', lineHeight: 1.2 }}>
            <Typography
              sx={{
                fontFamily: "'JetBrains Mono', monospace",
                fontSize: '0.75rem',
                fontWeight: 500,
                color: '#d1d4dc',
                letterSpacing: '0.03em',
              }}
            >
              {formatTime(clock)}
            </Typography>
            <Typography
              sx={{
                fontSize: '0.625rem',
                color: '#5d606b',
                fontWeight: 400,
              }}
            >
              {formatDate(clock)}
            </Typography>
          </Box>
        </Box>
      </Toolbar>
    </AppBar>
  );
}
