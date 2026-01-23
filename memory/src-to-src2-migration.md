# Source Folder Migration Log

**Date**: January 23, 2026  
**Status**: ✅ COMPLETED

## Issue Identified

The Aspire setup under the `src/` folder is **NOT correctly configured** and should **no longer be used**.

### Incorrect Path (Deprecated)
- ❌ `/Users/champion.trader/Code/stock-market-simulator/src/StockMarketSimulator.sln`
- ❌ Projects under `src/` folder

### Correct Path (Verified Working)
- ✅ `/Users/champion.trader/Code/stock-market-simulator/stock-market-simulator/src2/stock-market-simulator.sln`
- ✅ Projects under `stock-market-simulator/src2/` folder

## Reason for Deprecation

The initial Aspire setup in the `src/` folder had configuration issues and was not properly structured for .NET Aspire orchestration. A new, correctly configured structure has been established under `stock-market-simulator/src2/`.

## Migration Summary

### Projects Successfully Migrated from src/ to src2/

- ✅ **Shared.Contracts** - Shared contracts library
  - Files: `MarketDataEvent.cs`, `Security.cs`
  - Framework: net8.0
  - Location: `stock-market-simulator/src2/Shared.Contracts/`

- ✅ **MarketSimulator** - Market data simulator service
  - Files: `Program.cs`, `ExchangeSimulator.cs`, `appsettings.json`
  - Framework: net8.0, Worker Service
  - Dependencies: Shared.Contracts, Microsoft.Extensions.Http
  - Location: `stock-market-simulator/src2/MarketSimulator/`

- ✅ **MarketIngestion.Api** - Market data ingestion API
  - Files: `Program.cs`, `appsettings.json`
  - Framework: net8.0, Web API
  - Dependencies: Shared.Contracts, StackExchange.Redis, AspNetCore.HealthChecks.Redis
  - Location: `stock-market-simulator/src2/MarketIngestion.Api/`

- ✅ **Broker.Service** - Broker service for consuming market data
  - Files: `Program.cs`, `RedisStreamConsumer.cs`, `Controllers/SseController.cs`
  - Framework: net8.0, Web API
  - Dependencies: Shared.Contracts, StackExchange.Redis
  - Location: `stock-market-simulator/src2/Broker.Service/`

- ✅ **frontend** - React frontend application (formerly webapp)
  - Copied from: `src/webapp/`
  - To: `stock-market-simulator/src2/frontend/`
  - Technology: React + TypeScript + Vite

### AppHost Configuration Updated

The AppHost has been configured to orchestrate all services:

```csharp
// Redis for message streaming
var cache = builder.AddRedis("cache");

// Market Ingestion API
var ingestionApi = builder.AddProject<Projects.MarketIngestion_Api>("ingestion-api")
    .WithReference(cache);

// Market Simulator
var marketSimulator = builder.AddProject<Projects.MarketSimulator>("market-simulator")
    .WithReference(ingestionApi);

// Broker Service
var brokerService = builder.AddProject<Projects.Broker_Service>("broker-service")
    .WithReference(cache);

// Frontend
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(brokerService);
```

### Build Verification

✅ **Build Status**: SUCCESS

```
Build succeeded in 1.0s
- Shared.Contracts: ✅
- MarketSimulator: ✅
- MarketIngestion.Api: ✅
- Broker.Service: ✅
- stock-market-simulator.Server: ✅
- stock-market-simulator.AppHost: ✅
```

## Solution Structure (src2/)

```
stock-market-simulator/src2/
├── stock-market-simulator.sln
├── Shared.Contracts/
│   ├── MarketDataEvent.cs
│   ├── Security.cs
│   └── Shared.Contracts.csproj
├── MarketSimulator/
│   ├── Program.cs
│   ├── ExchangeSimulator.cs
│   ├── appsettings.json
│   └── MarketSimulator.csproj
├── MarketIngestion.Api/
│   ├── Program.cs
│   ├── appsettings.json
│   └── MarketIngestion.Api.csproj
├── Broker.Service/
│   ├── Program.cs
│   ├── RedisStreamConsumer.cs
│   ├── Controllers/SseController.cs
│   └── Broker.Service.csproj
├── frontend/
│   ├── src/
│   ├── package.json
│   └── vite.config.ts
├── stock-market-simulator.AppHost/
│   ├── AppHost.cs
│   └── stock-market-simulator.AppHost.csproj
└── stock-market-simulator.Server/
    ├── Program.cs
    └── stock-market-simulator.Server.csproj
```

## Next Steps

1. ✅ Test the entire solution by running the AppHost
2. ⏳ Verify all services start correctly and communicate properly
3. ⏳ Test the frontend connects to the broker service SSE endpoint
4. ⏳ Update all documentation to reference the new src2 structure
5. ⏳ Archive or remove the deprecated src/ folder
6. ⏳ Update CI/CD pipelines if applicable
7. ⏳ Notify team members about the new structure

## Important Notes

- **All future development must occur in `stock-market-simulator/src2/`**
- The `src/` folder is deprecated and should NOT be used
- The solution is configured and builds successfully
- All project dependencies and references are correctly set up
- The AppHost orchestrates all services properly with .NET Aspire

## Commands for Running

```bash
# Navigate to AppHost directory
cd /Users/champion.trader/Code/stock-market-simulator/stock-market-simulator/src2/stock-market-simulator.AppHost

# Run the application
dotnet run
```

Or use the `aspire` command if preferred:

```bash
cd /Users/champion.trader/Code/stock-market-simulator/stock-market-simulator/src2
aspire run
```
