# Stock Market Simulator

A real-time stock market data simulation system built with .NET 10 and Aspire, demonstrating streaming architectures, Redis pub/sub, and Server-Sent Events (SSE).

## Quick Start

### Prerequisites
- .NET 10.0.102+ SDK
- Docker Desktop

### Running the Application

```bash
# 1. Start Redis and build all projects
./run-services.sh

# 2. Start services in separate terminals:

# Terminal 1 - Market Ingestion API
cd src/MarketIngestion.Api
export PATH="$HOME/.dotnet:$PATH"
dotnet run --urls "http://localhost:5001"

# Terminal 2 - Broker Service
cd src/Broker.Service
export PATH="$HOME/.dotnet:$PATH"
dotnet run --urls "http://localhost:5002"

# Terminal 3 - Market Simulator
cd src/MarketSimulator
export PATH="$HOME/.dotnet:$PATH"
dotnet run

# Terminal 4 - Web Frontend (optional)
cd src/frontend
npm install
npm run dev
```

### Service Endpoints
- **Market Ingestion API:** http://localhost:5001
- **Broker Service (SSE):** http://localhost:5002
- **Web Frontend:** http://localhost:5173 (Vite dev server)
- **Redis:** localhost:6379

## Architecture

The system simulates a stock market data pipeline:

1. **MarketSimulator** - Generates synthetic market data (price updates, trades)
2. **MarketIngestion.Api** - Receives market data via HTTP and publishes to Redis
3. **Redis** - Message broker for real-time data distribution
4. **Broker.Service** - Consumes from Redis and streams data to clients via Server-Sent Events
5. **Web Frontend** - React/TypeScript UI displaying real-time market data with candlestick charts

## Technology Stack

### Backend
- **.NET 10** - Latest framework with performance improvements
- **Aspire 13.1.0** - Cloud-native orchestration
- **Redis** - In-memory data store and message broker
- **StackExchange.Redis** - Redis client library
- **Server-Sent Events** - Real-time streaming to web clients

### Frontend
- **React 19** - UI framework
- **TypeScript 5.9** - Type safety
- **Material UI 7** - Component library
- **D3.js 7** - Candlestick charts
- **Vite 7** - Build tool

## Project Structure

```
src/
├── AppHost/                  # Aspire orchestration host
├── Shared.Contracts/         # Shared data models
├── MarketSimulator/          # Market data generator
├── MarketIngestion.Api/      # Data ingestion endpoint
├── Broker.Service/           # SSE streaming service
└── frontend/                 # React/TypeScript web UI
```

## Documentation

### Architecture Documentation
- [Architecture Overview](/docs/architecture/README.md) - **Start here** for comprehensive documentation
- [High-Level Design](/docs/architecture/HIGH_LEVEL_DESIGN.md) - System overview with diagrams
- [Low-Level Design](/docs/architecture/LOW_LEVEL_DESIGN.md) - Detailed component specifications
- [C4 Diagrams](/docs/architecture/C4_DIAGRAMS.md) - Visual architecture diagrams in Mermaid format
- [Architecture Decision Records](/docs/architecture/ADR/README.md) - Key design decisions

### Requirements & Planning
- [Product Requirements](/docs/requirements/prd.md) - Product requirements document
- [Technical Approach](/docs/plan/draft-technical-approach.md) - Implementation guide

### Development Notes
- [Project Status](/memory/dotnet10-status.md) - Current project status
- [.NET 10 Upgrade Summary](/memory/dotnet10-upgrade-summary.md) - .NET 10 upgrade details
- [Orchestration Setup](/memory/aspire-orchestration-resolution.md) - Aspire orchestration resolution

## Development Status

✅ **Complete** - All services build and run successfully  
⚠️ **Note:** Aspire Dashboard unavailable due to .NET 10/Aspire 13.1.0 DCP limitations. Services run independently via helper script.

## Building

```bash
cd src
export PATH="$HOME/.dotnet:$PATH"
dotnet build
```

## Testing

Start Redis:
```bash
docker run -d --name test-redis -p 6379:6379 redis:latest
```

Run individual services with `dotnet run` in their respective directories.

## License

See [LICENSE](LICENSE) for details.
