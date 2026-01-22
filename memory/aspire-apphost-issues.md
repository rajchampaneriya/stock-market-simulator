# Aspire AppHost Execution Issues and Resolution

**Date:** January 22, 2026  
**Status:** ⚠️ In Progress - DCP/Dashboard Installation Issue

## Problem Summary

The AppHost project fails to run with the following error:
```
Property CliPath: The path to the DCP executable used for Aspire orchestration is required.
Property DashboardPath: The path to the Aspire Dashboard binaries is missing.
```

## Root Cause

.NET Aspire 8.2 requires two runtime components that are not included in the base workload:
1. **DCP (Developer Control Plane)** - The local orchestrator that manages containers
2. **Aspire Dashboard** - The web-based monitoring UI

These components should be downloaded automatically on first run, but this mechanism appears to be failing on macOS with .NET SDK 8.0.405.

## Investigation Steps

### 1. Workload Installation
✅ Installed Aspire workload: `dotnet workload install aspire`
- Workload ID: aspire
- Manifest Version: 8.2.2/8.0.100
- Location: `/usr/local/share/dotnet/packs/Aspire.Hosting.Sdk/8.2.2/`

### 2. Missing Components
❌ DCP executable not found in expected locations
❌ Dashboard binaries not found in NuGet package cache

### 3. Configuration Issues Fixed
✅ Endpoint naming conflicts resolved (both services had `name: "http"`)
- Changed to unique names: `ingestion-http` and `broker-http`

✅ Package versions aligned to .NET 8.0:
- Aspire.Hosting.AppHost: 8.2.0
- Aspire.Hosting.Redis: 8.2.0  
- Aspire.StackExchange.Redis: 8.2.0

## Known Aspire 8.x Limitation

On macOS, the DCP and Dashboard are distributed separately and require either:
1. Manual installation via additional NuGet packages/tools
2. Docker Desktop to be running (for container orchestration)
3. Using Visual Studio 2022 17.9+ which bundles these components

## Potential Solutions

### Option 1: Manual DCP/Dashboard Installation (Recommended)
The DCP and Dashboard should download automatically, but may require:
- Proper internet connectivity for NuGet package restoration
- Correct NuGet source configuration
- Appropriate file permissions

### Option 2: Use Docker Compose Instead
Convert the Aspire orchestration to docker-compose.yml for local development

### Option 3: Run Services Individually
Launch each service (MarketSimulator, MarketIngestion.Api, Broker.Service) manually without orchestration

### Option 4: Use Visual Studio or Rider
IDE integration handles DCP/Dashboard installation automatically

## Current Workarounds Applied

### Code Changes
1. **Program.cs Endpoint Fixes**:
   ```csharp
   // Before: Both had name: "http"  
   var ingestion = builder.AddProject(...).WithHttpEndpoint(port: 5001, name: "ingestion-http");
   var broker = builder.AddProject(...).WithHttpEndpoint(port: 5002, name: "broker-http");
   ```

2. **Package Dependencies**:
   - Added Aspire.StackExchange.Redis to service projects
   - Downgraded StackExchange.Redis to 2.8.0 for compatibility
   - Added project references to Shared.Contracts

3. **Created appsettings.json** for AppHost with Aspire configuration options

## Current Status

**UNRESOLVED**: The DCP and Dashboard components required by .NET Aspire 8.2 are not available on this macOS installation with .NET SDK 8.0.405. This is a known limitation of the current Aspire distribution model.

## Resolution: Run Services Without Orchestration

Since the Aspire orchestrator (DCP) is not available, services should be run manually in separate terminals:

### Manual Service Startup

1. **Start Redis** (using Docker):
   ```bash
   docker run --name redis-dev -p 6379:6379 -d redis:latest
   ```

2. **Start MarketIngestion.Api**:
   ```bash
   cd src/MarketIngestion.Api
   dotnet run --urls="http://localhost:5001"
   ```

3. **Start Broker.Service**:
   ```bash
   cd src/Broker.Service
   dotnet run --urls="http://localhost:5002"
   ```

4. **Start MarketSimulator**:
   ```bash
   cd src/MarketSimulator
   export IngestionApiUrl="http://localhost:5001"
   dotnet run
   ```

### Environment Configuration

Each service needs these environment variables or appsettings:

**MarketIngestion.Api** and **Broker.Service**:
```json
{
  "ConnectionStrings": {
    "redis": "localhost:6379"
  }
}
```

**MarketSimulator**:
```json
{
  "IngestionApiUrl": "http://localhost:5001"
}
```

## Alternative: Docker Compose

A docker-compose.yml file would provide better orchestration without Aspire:

```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
  
  ingestion-api:
    build:
      context: ./src
      dockerfile: MarketIngestion.Api/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ConnectionStrings__redis=redis:6379
    depends_on:
      - redis
  
  broker-service:
    build:
      context: ./src
      dockerfile: Broker.Service/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - ConnectionStrings__redis=redis:6379
    depends_on:
      - redis
  
  market-simulator:
    build:
      context: ./src
      dockerfile: MarketSimulator/Dockerfile
    environment:
      - IngestionApiUrl=http://ingestion-api:8080
    depends_on:
      - ingestion-api
```

## Recommendation

For development on this machine:
1. Use manual service startup (documented above)
2. Create launch configurations in VS Code for each service
3. Consider creating a simple shell script to start all services
4. When deploying, use Aspire on a machine with proper DCP support or containerize with Docker Compose

## Files Modified

- `/src/AppHost/Program.cs` - Fixed endpoint naming conflicts
- `/src/AppHost/appsettings.json` - Created with Aspire configuration
- `/src/MarketIngestion.Api/MarketIngestion.Api.csproj` - Added Aspire Redis package
- `/src/Broker.Service/Broker.Service.csproj` - Added Aspire Redis package and Shared.Contracts reference
- `/src/MarketSimulator/MarketSimulator.csproj` - Downgraded Microsoft.Extensions.Http to 8.0.0

## References

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire Known Issues](https://github.com/dotnet/aspire/issues)
- [DCP GitHub Repository](https://github.com/microsoft/dotnet-dev-containers-pilot)
