import {
  Container,
  Typography,
  Box,
  Alert,
  Grid,
  CircularProgress,
} from '@mui/material';
import { useMarketData } from './hooks/useMarketData';
import { StockCard } from './components/StockCard';
import './App.css';

function App() {
  const brokerUrl = import.meta.env.VITE_BROKER_URL || 'http://localhost:5002';
  const { securities, isConnected, error } = useMarketData(brokerUrl);

  const securitiesArray = Array.from(securities.values());

  return (
    <Container maxWidth="lg">
      <Box sx={{ my: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom>
          Stock Market Simulator
        </Typography>
        <Typography variant="subtitle1" color="text.secondary" gutterBottom>
          Real-time market data streaming
        </Typography>

        {error && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {isConnected && securitiesArray.length === 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
            <CircularProgress />
            <Typography sx={{ ml: 2 }}>
              Waiting for market data...
            </Typography>
          </Box>
        )}

        {securitiesArray.length > 0 && (
          <Grid container spacing={3} sx={{ mt: 2 }}>
            {securitiesArray.map((security) => (
              <Grid size={{ xs: 12, sm: 6, md: 4, lg: 3 }} key={security.symbol}>
                <StockCard security={security} />
              </Grid>
            ))}
          </Grid>
        )}

        {isConnected && (
          <Box sx={{ mt: 4, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              Tracking {securitiesArray.length} securities in real-time
            </Typography>
          </Box>
        )}
      </Box>
    </Container>
  );
}

export default App;

