# Task 10: Create React App with TypeScript

## Objective
Set up the React frontend application with TypeScript, Material UI, and necessary dependencies.

## Prerequisites
- Node.js 18+ installed
- npm or yarn available
- WebApp directory exists in project structure

## Acceptance Criteria
- [ ] React app created with TypeScript template
- [ ] Material UI (MUI) installed and configured
- [ ] d3.js installed for future charting
- [ ] TypeScript configured properly
- [ ] ESLint and prettier configured
- [ ] Development server runs successfully
- [ ] Basic app structure created
- [ ] Environment variables configured for API URLs

## Implementation Steps

### 1. Create React App
```bash
cd src
npx create-react-app WebApp --template typescript
cd WebApp
```

### 2. Install Dependencies
```bash
# Material UI
npm install @mui/material @mui/icons-material @emotion/react @emotion/styled

# d3.js for charts
npm install d3 @types/d3

# Additional utilities
npm install date-fns
```

### 3. Configure Environment Variables
Create `src/WebApp/.env`:
```env
VITE_BROKER_URL=http://localhost:5002
```

### 4. Update package.json Scripts
```json
{
  "scripts": {
    "start": "react-scripts start",
    "build": "react-scripts build",
    "test": "react-scripts test",
    "eject": "react-scripts eject"
  }
}
```

### 5. Create Basic Folder Structure
```
src/WebApp/src/
├── components/
│   └── StockCard.tsx
├── hooks/
│   └── useMarketData.ts
├── types/
│   └── index.ts
├── App.tsx
├── App.css
└── index.tsx
```

### 6. Create Type Definitions
**src/WebApp/src/types/index.ts**
```typescript
export interface MarketDataEvent {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export interface Security {
  symbol: string;
  lastPrice: number;
  previousPrice: number;
  volume: number;
  lastUpdate: string;
  change: number;
  changePercent: number;
}
```

### 7. Basic App.tsx
```typescript
import React from 'react';
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
```

## Verification
```bash
cd src/WebApp
npm start
```

Expected:
- App opens at http://localhost:3000
- Material UI theme is applied
- No console errors
- TypeScript compilation succeeds

## Related Sections
- Technical Approach Section 2 (Technology Stack)
- Technical Approach Section 4 (Project Structure)
- Technical Approach Section 7 (Phase 4: UI)

## Estimated Effort
1-2 hours
