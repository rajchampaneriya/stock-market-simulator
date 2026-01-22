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
```

### Service Endpoints
- **Market Ingestion API:** http://localhost:5001
- **Broker Service (SSE):** http://localhost:5002
- **Redis:** localhost:6379

## Architecture

The system simulates a stock market data pipeline:

1. **MarketSimulator** - Generates synthetic market data (price updates, trades)
2. **MarketIngestion.Api** - Receives market data via HTTP and publishes to Redis
3. **Redis** - Message broker for real-time data distribution
4. **Broker.Service** - Consumes from Redis and streams data to clients via Server-Sent Events

## Technology Stack

- **.NET 10** - Latest framework with performance improvements
- **Aspire 9.5.2** - Cloud-native orchestration (structure preserved)
- **Redis** - In-memory data store and message broker
- **StackExchange.Redis** - Redis client library
- **Server-Sent Events** - Real-time streaming to web clients

## Project Structure

```
src/
├── AppHost/                  # Aspire orchestration host
├── Shared.Contracts/         # Shared data models
├── MarketSimulator/          # Market data generator
├── MarketIngestion.Api/      # Data ingestion endpoint
└── Broker.Service/           # SSE streaming service
```

## Documentation

- [/memory/dotnet10-status.md](/memory/dotnet10-status.md) - Current project status
- [/memory/dotnet10-upgrade-summary.md](/memory/dotnet10-upgrade-summary.md) - .NET 10 upgrade details
- [/memory/aspire-orchestration-resolution.md](/memory/aspire-orchestration-resolution.md) - Orchestration setup
- [/docs/requirements/prd.md](/docs/requirements/prd.md) - Product requirements
- [/docs/plan/draft-technical-approach.md](/docs/plan/draft-technical-approach.md) - Technical design

## Development Status

✅ **Complete** - All services build and run successfully  
⚠️ **Note:** Aspire Dashboard unavailable due to .NET 10/Aspire 9.5.2 DCP limitations. Services run independently via helper script.

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

## Future Enhancements

- Docker Compose orchestration
- React/TypeScript frontend with real-time charts
- Candlestick chart visualization
- WebSocket support
- Performance testing and optimization

## License

See [LICENSE](LICENSE) for details.
