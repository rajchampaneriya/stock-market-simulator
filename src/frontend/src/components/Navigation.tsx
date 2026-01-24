import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import DashboardIcon from '@mui/icons-material/Dashboard';
import StorageIcon from '@mui/icons-material/Storage';

/**
 * Navigation component with tabs for Dashboard and Redis Monitoring
 */
export function Navigation() {
  const location = useLocation();

  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" component="div" sx={{ flexGrow: 0, mr: 4 }}>
          Stock Market Simulator
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            color="inherit"
            component={RouterLink}
            to="/"
            startIcon={<DashboardIcon />}
            variant={location.pathname === '/' ? 'outlined' : 'text'}
            sx={{
              borderColor: location.pathname === '/' ? 'white' : 'transparent',
            }}
          >
            Dashboard
          </Button>
          <Button
            color="inherit"
            component={RouterLink}
            to="/redis-monitoring"
            startIcon={<StorageIcon />}
            variant={location.pathname === '/redis-monitoring' ? 'outlined' : 'text'}
            sx={{
              borderColor: location.pathname === '/redis-monitoring' ? 'white' : 'transparent',
            }}
          >
            Redis Monitoring
          </Button>
        </Box>
      </Toolbar>
    </AppBar>
  );
}
