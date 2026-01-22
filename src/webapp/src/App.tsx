import { Container, Typography, Box } from '@mui/material';
import './App.css';

function App() {
  return (
    <Container maxWidth="lg">
      <Box sx={{ my: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom>
          Stock Market Simulator
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Real-time market data streaming
        </Typography>
      </Box>
    </Container>
  );
}

export default App;

