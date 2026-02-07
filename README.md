# Stock Market Simulator

A production-grade real-time streaming system that simulates financial market data using event-driven architecture. Built with .NET 10, Redis Streams, and React, this project demonstrates scalable patterns for high-frequency data distribution.

## Features

**Real-Time Data Pipeline**
- Sub-second tick-to-browser latency using Server-Sent Events (SSE)
- Redis Streams with consumer groups for horizontal scalability
- Event-driven architecture with bounded channels and backpressure handling

**Realistic Market Simulation**
- Geometric Brownian Motion with mean reversion (Ornstein-Uhlenbeck process)
- GARCH-inspired volatility clustering
- Configurable per-symbol price dynamics

**Professional UI**
- TradingView-inspired dark theme dashboard
- D3.js custom candlestick charts with interactive crosshair
- Live price updates, watchlist, and real-time OHLCV aggregation

**Production Patterns**
- OpenTelemetry distributed tracing
- Health checks and observability
- Retry logic with exponential backoff
- SSE reconnection with replay support

## Architecture

```
Market Simulator → Ingestion API → Redis Streams → Broker Service → SSE → React Frontend
                     (HTTP)          (Pub/Sub)       (Consumer)    (Push)   (EventSource)
```

The system consists of five services orchestrated by .NET Aspire:

| Service | Technology | Purpose |
|---|---|---|
| **Market Simulator** | .NET Worker | Generates realistic price ticks using quantitative finance models |
| **Ingestion API** | ASP.NET Minimal API | Validates and publishes market data to Redis Streams |
| **Broker Service** | ASP.NET Web API | Consumes from Redis, aggregates candles, streams via SSE |
| **Web Frontend** | React + TypeScript | Real-time trading dashboard with D3 charts |
| **Redis** | Redis 7+ | Message broker with stream support |

## Quick Start

### Prerequisites
- .NET 10.0.102+ SDK
- Docker Desktop
- Node.js 18+ (for frontend)

### Run All Services

```bash
# Start Redis
docker run -d --name redis -p 6379:6379 redis:latest

# Option 1: Use helper script
./run-services.sh

# Option 2: Manual start with Aspire (recommended)
cd src/stock-market-simulator.AppHost
dotnet run
```

The system will start in sequence:
1. Redis (port 6379)
2. Market Ingestion API (port 5001)
3. Broker Service (port 5002)  
4. Market Simulator (background)
5. Web Frontend (port 5173)

Open your browser to http://localhost:5173

### Verify It Works

You should see:
- 4 stock symbols (NIFTY50, BANKNIFTY, RELIANCE, TCS) in the ticker strip
- Prices updating every ~300ms with green/red flashes
- Candlestick chart building in real-time
- "LIVE" status indicator in the bottom bar

## Technology Stack

**Backend**
- .NET 10 with C# 13
- ASP.NET Core (Minimal APIs + Controllers)
- Redis Streams (StackExchange.Redis)
- .NET Aspire 13.1 orchestration
- OpenTelemetry with OTLP export

**Frontend**
- React 19 with TypeScript 5.9
- D3.js 7 for charts
- Material UI 7 components
- Vite 7 build tool
- EventSource API for SSE

## Documentation

📘 **[Complete Reference Documentation](reference-docs/README.md)** — Start here for comprehensive technical documentation

| Document | Description |
|---|---|
| [High-Level Design](reference-docs/01-HIGH-LEVEL-DESIGN.md) | System overview, component inventory, deployment topology |
| [Low-Level Design](reference-docs/02-LOW-LEVEL-DESIGN.md) | Detailed service design, class diagrams, sequences |
| [Architecture Decisions](reference-docs/03-ARCHITECTURE-DECISION-RECORDS.md) | 10 ADRs explaining key design choices |
| [C4 Diagrams](reference-docs/04-C4-DIAGRAMS.md) | Visual architecture with 20+ Mermaid diagrams |
| [Functional Guide](reference-docs/05-FUNCTIONAL-TECHNICAL-DOCUMENTATION.md) | Plain-English walkthrough for non-technical audiences |

## Project Structure

```
src/
├── stock-market-simulator.AppHost/    # Aspire orchestration
├── Shared.Contracts/                   # Data models (records)
├── MarketSimulator/                    # Price generation engine
├── MarketIngestion.Api/                # HTTP ingestion endpoint
├── Broker.Service/                     # Redis consumer + SSE server
│   ├── Controllers/                    # SSE and monitoring endpoints
│   └── Services/                       # Aggregation and history
└── frontend/                           # React/TypeScript dashboard
    ├── src/components/                 # Chart, cards, navigation
    ├── src/hooks/                      # useMarketData (SSE client)
    └── src/pages/                      # Dashboard, Redis monitoring
```

## Configuration

### Market Simulator

Edit `src/MarketSimulator/appsettings.json`:

```json
{
  "Symbols": ["NIFTY50", "BANKNIFTY", "RELIANCE", "TCS"],
  "TickIntervalMs": 300,
  "InitialPrice": 1000
}
```

### Broker Service

Edit `src/Broker.Service/appsettings.json`:

```json
{
  "Candlestick": {
    "IntervalSeconds": 60,
    "EmitThrottleMs": 500,
    "MaxHistory": 60
  }
}
```

## Key Design Patterns

**Bounded Channels with Backpressure**  
Uses `BoundedChannelFullMode.DropOldest` to prevent memory exhaustion. Acceptable to lose stale ticks rather than block producers.

**Fan-Out with TickBroadcaster**  
Single Redis consumer broadcasts to multiple channels (SSE + Aggregator) using non-blocking writes.

**SSE with Last-Event-ID Replay**  
`MarketEventBuffer` ring buffer enables clients to resume from last received event on reconnection.

**Backend Candlestick Aggregation**  
OHLCV candles built server-side for consistency. Throttled emission (500ms) prevents overwhelming clients.

**Realistic Price Generation**  
Combines GBM, mean reversion, and GARCH volatility to produce chart patterns indistinguishable from real market data.

## Observability

**Health Checks**
- Ingestion API: http://localhost:5001/health
- Broker Service: http://localhost:5002/health

**Redis Monitoring**
- Web UI: http://localhost:5173/redis-monitoring
- Shows stream message count, consumer groups, pending messages

**Distributed Tracing**
- OpenTelemetry instrumentation on all .NET services
- Console export enabled by default
- OTLP export configured (connect to Jaeger/Tempo/Azure Monitor)

## Troubleshooting

| Issue | Solution |
|---|---|
| Dashboard shows "Connecting..." | Verify Broker Service is running on port 5002 |
| Chart is empty | Wait 60 seconds for first candle to complete |
| Prices not updating | Check Market Simulator is running and posting to Ingestion API |
| Redis connection errors | Ensure Redis container is running: `docker ps` |

For detailed troubleshooting, see the [Functional Documentation](reference-docs/05-FUNCTIONAL-TECHNICAL-DOCUMENTATION.md#9-monitoring-and-diagnostics).

## Building

```bash
cd src
dotnet build
```

All services target .NET 10.0. Frontend requires `npm install` in `src/frontend/`.

## Testing

```bash
# Start dependencies
docker run -d --name test-redis -p 6379:6379 redis:latest

# Run tests
cd tests
dotnet test
```

## Why This Stack?

**Redis Streams over Kafka** — Simpler operational model, built-in consumer groups, already used for caching. Suitable for single-DC workloads.

**SSE over WebSockets** — Unidirectional streaming is sufficient. Browser `EventSource` handles reconnection. Works with standard HTTP infrastructure.

**.NET Aspire** — Service discovery, dependency ordering, and connection injection without Docker Compose complexity.

**Backend Aggregation** — Single source of truth for candles. Reduces frontend complexity and CPU usage.

See [Architecture Decision Records](reference-docs/03-ARCHITECTURE-DECISION-RECORDS.md) for detailed rationale.

## License

MIT License - see [LICENSE](LICENSE) for details.

---

## About

This project demonstrates production-grade patterns for real-time data streaming systems:
- Event-driven architecture with Redis Streams
- Server-Sent Events for efficient server-push
- Realistic financial data simulation using quantitative models
- Bounded channels and backpressure handling
- Distributed tracing and observability

Built as a technical showcase of modern .NET development practices and streaming architectures.

---

## Acknowledgments

Documentation and architectural refinement assisted by [GitHub Copilot](https://github.com/features/copilot).
