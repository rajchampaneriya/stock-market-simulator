# System Validation and Testing Plan

**Date**: January 23, 2026  
**Status**: In Progress

## Issues Identified and Fixed

### Issue 1: Broker Service Health Check ✅ FIXED
- **Problem**: Missing health check configuration
- **Solution**: Added health checks to Broker.Service
- **Files Modified**: Broker.Service/Program.cs, Broker.Service.csproj
- **Status**: ✅ Resolved

### Issue 2: Incorrect Frontend Copied ✅ FIXED
- **Problem**: Default Vite template copied instead of actual stock market webapp
- **Root Cause**: The src2 structure had a template frontend, not the actual stock market app
- **Solution**: Replaced with correct webapp from src/webapp
- **Details**: 
  - Original frontend was basic weather app template
  - Should be stock market app with useMarketData hook, StockCard components
  - Removed old frontend and copied correct src/webapp
  - Removed frontend.esproj from solution (not needed for Vite apps in Aspire)
- **Status**: ✅ Fixed - Correct frontend now in place

### Issue 3: Missing Frontend Project File ✅ FIXED
- **Problem**: Solution referenced frontend.esproj which didn't exist
- **Solution**: Removed frontend.esproj from solution file
- **Reason**: Vite frontend managed by Aspire's AddViteApp(), doesn't need .esproj
- **Status**: ✅ Resolved

## Validation Checklist

### Component Validation

#### 1. Shared.Contracts ✅
- [x] Build successfully
- [x] MarketDataEvent class accessible
- [x] Security class accessible
- [x] Used by other projects correctly

#### 2. MarketSimulator ✅
- [x] Builds successfully
- [x] Connects to MarketIngestion.Api
- [x] Configuration present (symbols, interval, volatility)
- [x] Error handling and retry logic present

#### 3. MarketIngestion.Api ✅
- [x] Builds successfully
- [x] Health check endpoint configured
- [x] Market data POST endpoint defined
- [x] Redis connection configured
- [x] Publishes to Redis stream

#### 4. Broker.Service ✅
- [x] Builds successfully
- [x] Health check endpoint configured ✅
- [x] Redis connection configured
- [x] Redis stream consumer implemented
- [x] SSE endpoint available at /api/marketdata/stream
- [x] CORS configured correctly

#### 5. Frontend (React/Vite) ✅
- [x] Correct stock market app in place ✅
- [x] useMarketData hook present
- [x] StockCard components present
- [x] Material-UI dependencies in package.json
- [x] Connects to broker-service SSE endpoint
- [x] Price history and charts configured

### Integration Tests

#### Data Flow Test ⏳
```
MarketSimulator → MarketIngestion.Api → Redis → Broker.Service → Frontend (SSE)
```

- [ ] Simulator generates data
- [ ] Ingestion API receives data
- [ ] Data persists in Redis stream
- [ ] Broker consumes from stream
- [ ] Frontend receives SSE events
- [ ] UI updates in real-time

#### Service Startup Order ⏳
- [ ] Redis starts first
- [ ] MarketIngestion.Api waits for Redis
- [ ] MarketSimulator waits for Ingestion API
- [ ] Broker.Service waits for Redis
- [ ] Frontend waits for Broker.Service

#### Health Checks ⏳
- [ ] All services report healthy
- [ ] Health checks include dependencies
- [ ] Unhealthy services block dependents

### Configuration Validation

#### Redis Configuration ⏳
- [ ] Connection string consistent across services
- [ ] Stream name matches (marketdata:stream)
- [ ] Consumer group configured (brokers)

#### API Endpoints ⏳
- [ ] MarketIngestion.Api: POST /api/marketdata
- [ ] MarketIngestion.Api: GET /health
- [ ] Broker.Service: GET /api/marketdata/stream (SSE)
- [ ] Broker.Service: GET /health

#### Environment Variables ⏳
- [ ] VITE_BROKER_URL configured for frontend
- [ ] Redis connection strings
- [ ] Service URLs

## Test Execution Plan

### Phase 1: Build Verification
```bash
cd /Users/champion.trader/Code/stock-market-simulator/stock-market-simulator/src2
dotnet build
```

### Phase 2: Run AppHost
```bash
cd stock-market-simulator.AppHost
dotnet run
```

### Phase 3: Manual Service Tests
1. Check Aspire Dashboard - all services healthy
2. Test MarketIngestion.Api Swagger: https://ingestion-api-stock_market_simulator.dev.localhost/swagger
3. Test Broker.Service Swagger: https://broker-service-stock_market_simulator.dev.localhost/swagger
4. Test SSE endpoint: curl to /api/marketdata/stream
5. Open Frontend and verify real-time data

### Phase 4: End-to-End Test
1. Verify market data flowing from simulator
2. Check Redis has data (docker exec or redis-cli)
3. Verify broker consuming and transforming data
4. Confirm frontend receives and displays data

## Known Issues to Monitor

1. ⚠️ Frontend HTTP 500 - likely due to wrong webapp being copied (NOW FIXED)
2. ⚠️ Port conflicts or URL mismatches
3. ⚠️ CORS issues between services
4. ⚠️ Redis connection timing

## Success Criteria

✅ All projects build without errors
✅ All services start and become healthy
✅ Data flows through entire pipeline
✅ Frontend displays real-time stock prices
✅ No errors in Aspire dashboard
✅ All health checks pass

## Next Actions

1. Restart AppHost with correct frontend
2. Run full system test
3. Document any remaining issues
4. Create final validation report
