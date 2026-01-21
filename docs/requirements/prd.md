# Product Requirements Document (PRD): Stock Market Data Streaming System

**Version:** 1.0  
**Date:** January 2026  
**Author:** Raj Champaneriya  
**Based on:** [objective.md](./objective.md)

## 1. Introduction

### 1.1 Purpose

This document provides detailed engineering requirements for the Stock Market Data Streaming System - a **minimal but realistic prototype** that demonstrates how real-time stock market data flows from exchanges through brokers to end users. 

This PRD elaborates on the high-level vision outlined in [objective.md](./objective.md), providing the technical specifications needed for implementation.

The system serves as both an educational tool and a technical demonstration of real-time data streaming architectures used in financial markets, specifically showcasing:
- Event-driven streaming architectures
- Fan-out data distribution patterns
- Near real-time data delivery mechanisms
- Modern .NET + Aspire capabilities

### 1.2 Scope

This is **not a trading system**. This is an **architecture + streaming demonstration**.

The minimal prototype will simulate:
- Market data generation from stock exchanges (Exchange Simulator)
- Data ingestion and validation (Market Ingestion Service)
- Event streaming layer (Redis Streams)
- Broker service for data distribution (Broker API)
- Real-time data delivery to web clients via Server-Sent Events
- The underlying infrastructure supporting near real-time data streaming

**Out of Scope for MVP:**
- Multiple broker types (retail, institutional, proprietary) - see Future Enhancements
- Mobile applications - see Future Enhancements
- Real market data integration - simulation only
- Order execution or trading functionality

### 1.3 Target Audience

- Engineers and Software Architects
- Hiring Managers evaluating technical capabilities
- Trading Technology Enthusiasts
- Developers interested in real-time streaming systems
- Students learning about event-driven architectures

## 2. System Overview

### 2.1 High-Level Architecture

The system consists of the following main components (aligned with [objective.md](./objective.md)):

```
┌─────────────────────┐
│ Market Data         │  (Exchange Simulator - .NET Worker Service)
│ Generator           │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Market Ingestion    │  (.NET 10 Minimal API)
│ Service             │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Event Stream        │  (Redis Streams)
└──────────┬──────────┘
           │
     ┌─────┴───────────────────┐
     │                         │
     ▼                         ▼
┌─────────────┐      ┌─────────────────┐
│ Broker      │      │ Analytics       │
│ Service     │      │ Engine          │
│ (SSE)       │      │ (Future)        │
└──────┬──────┘      └─────────────────┘
       │
       ▼
┌─────────────┐
│ Web UI      │  (ReactJS + SSE)
│ (Live       │
│  Prices)    │
└─────────────┘
```

**Core Components:**
1. **Market Data Generator (Exchange Simulator)**: Generates realistic market data ticks
2. **Market Ingestion Service**: Receives, validates, and publishes data to stream
3. **Event Stream Layer**: Redis Streams for event distribution
4. **Broker Service**: Subscribes to stream, caches prices, serves web clients via SSE
5. **Web UI**: Visualizes real-time streaming data

### 2.2 Data Flow

The end-to-end data flow follows this sequence:

1. **Market Data Generator** produces simulated stock ticks (price, volume, timestamp)
2. **Market Ingestion Service** receives ticks via HTTP POST, validates format
3. **Redis Streams** acts as the event distribution backbone
4. **Broker Service** subscribes to Redis, maintains latest price cache
5. **Web UI** connects via Server-Sent Events (SSE) for real-time updates
6. **Analytics Engine** (optional/future) can subscribe for aggregation and analysis

```
[Exchange Simulator]
        │
        ▼ (HTTP POST)
[Ingestion API]
        │
        ▼ (Publish)
[Redis Stream]
        │
        ▼ (Subscribe)
[Broker Service]
        │
        ▼ (SSE)
[Web UI]
```

### 2.3 Technology Stack

- **Platform**: .NET 10
- **Framework**: .NET Aspire 9.5 (for orchestration, service discovery, observability)
- **Streaming Layer**: Redis Streams (simple, fast, demo-appropriate)
- **Communication Protocol**: Server-Sent Events (SSE) for web client push
- **Backend**: .NET 10 Minimal API, Worker Services, BackgroundService
- **Frontend**: ReactJS, TypeScript, HTML5, CSS3, Material UI
- **Visualization**: d3.js for candlestick charts with volume
- **Observability**: OpenTelemetry (built into Aspire)
- **Caching**: Redis (for latest prices)
- **Deployment**: Docker + Aspire, local-first approach

**Why These Choices:**
- **Redis Streams**: Simpler than Kafka for demonstration, provides reliable event streaming
- **SSE**: Built into HTTP, simpler than WebSockets for one-way push
- **Aspire**: Provides service discovery, orchestration, and observability out-of-the-box
- **Local-first**: Low cost, easy to run, suitable for portfolio demonstration

## 3. Functional Requirements

### 3.1 Market Data Generator (Exchange Simulator)

#### 3.1.1 Data Generation

- **FR-1.1**: The system shall generate realistic market data for configurable stock symbols (default: NIFTY50, BANKNIFTY, etc.)
- **FR-1.2**: Market data shall include:
  - Symbol identifier
  - Price (with realistic volatility)
  - Volume
  - Timestamp (ISO 8601 format)
- **FR-1.3**: Data generation frequency shall be configurable (default: 200-500ms per tick)
- **FR-1.4**: Price movements shall follow realistic random walk patterns

**Sample Event:**
```json
{
  "symbol": "NIFTY50",
  "price": 22435.50,
  "volume": 1200,
  "timestamp": "2026-01-21T10:15:23.232Z"
}
```

#### 3.1.2 Data Publishing

- **FR-1.5**: The system shall publish market data events to the Market Ingestion Service via HTTP POST
- **FR-1.6**: Publishing shall be asynchronous to avoid blocking the generator
- **FR-1.7**: The generator shall run as a .NET Worker Service (BackgroundService)

### 3.2 Market Ingestion Service

#### 3.2.1 Data Reception

- **FR-2.1**: The system shall expose a minimal HTTP API endpoint to receive market data (POST /api/marketdata)
- **FR-2.2**: The system shall validate incoming data format and reject invalid requests
- **FR-2.3**: The system shall handle high-frequency data without loss (buffering if needed)

#### 3.2.2 Data Publishing to Stream

- **FR-2.4**: The system shall publish validated market data to Redis Streams
- **FR-2.5**: Events shall be published to a named stream (e.g., "marketdata:stream")
- **FR-2.6**: The system shall use Aspire service discovery to locate Redis
- **FR-2.7**: The system shall emit OpenTelemetry traces for observability

### 3.3 Broker Service

#### 3.3.1 Core Functionality

- **FR-3.1**: The system shall subscribe to Redis Streams for market data events
- **FR-3.2**: The system shall maintain an in-memory cache of latest prices per symbol
- **FR-3.3**: The system shall serve web clients via Server-Sent Events (SSE)
- **FR-3.4**: The system shall support symbol-based subscriptions from clients
- **FR-3.5**: The system shall apply rate-limiting/throttling logic to prevent overwhelming clients
- **FR-3.6**: The system shall provide snapshot data to new connections (latest prices)
- **FR-3.7**: The system shall send periodic heartbeats to maintain connections

#### 3.3.2 Connection Management

- **FR-3.8**: The system shall handle client disconnections gracefully
- **FR-3.9**: The system shall support auto-reconnection from clients
- **FR-3.10**: The system shall track active client connections
- **FR-3.11**: The system shall log connection events for monitoring

### 3.4 Web UI Client

#### 3.4.1 Core Features

- **FR-4.1**: The system shall provide a web-based client using ReactJS
- **FR-4.2**: The client shall connect to the Broker Service via Server-Sent Events
- **FR-4.3**: The client shall display real-time market data in a user-friendly format
- **FR-4.4**: The client shall allow users to select which symbols to follow
- **FR-4.5**: The client shall show price changes with visual indicators (color coding: green for up, red for down)
- **FR-4.6**: The client shall display a candlestick chart with volume using d3.js
- **FR-4.7**: The client shall show connection status (connected/disconnected)
- **FR-4.8**: The client shall display latency indicators
- **FR-4.9**: The client shall show a log of important events (price updates, connection status)
- **FR-4.10**: The client shall implement auto-reconnection on connection loss

## 4. Non-Functional Requirements

### 4.1 Performance

- **NFR-1.1**: End-to-end latency from exchange simulator to web client shall be **< 300 milliseconds** (p95)
- **NFR-1.2**: The system shall handle market data updates at a frequency of at least 10 events per second per symbol
- **NFR-1.3**: The Market Ingestion Service shall process incoming requests with < 50ms latency
- **NFR-1.4**: The Broker Service shall broadcast updates to clients within 100ms of receiving from stream

### 4.2 Scalability

- **NFR-2.1**: The system shall support at least **10,000+ simulated concurrent users** in testing scenarios
- **NFR-2.2**: The system shall be designed to scale horizontally by adding more broker service instances
- **NFR-2.3**: The system shall handle increasing data volumes without significant performance degradation
- **NFR-2.4**: The system shall support adding new symbols without requiring system restart

### 4.3 Reliability

- **NFR-3.1**: The system shall maintain data integrity during processing
- **NFR-3.2**: The system shall recover gracefully from connection failures
- **NFR-3.3**: The system shall provide auto-reconnection mechanisms for web clients
- **NFR-3.4**: The system shall handle Redis unavailability gracefully (retry with backoff)
- **NFR-3.5**: The system shall not lose events during normal operation

### 4.4 Observability

- **NFR-4.1**: The system shall emit OpenTelemetry traces for all operations
- **NFR-4.2**: The system shall track metrics: message throughput, connection count, latency percentiles
- **NFR-4.3**: The system shall provide request tracing across service boundaries
- **NFR-4.4**: The system shall log important events (errors, connection changes, performance issues)
- **NFR-4.5**: The Aspire dashboard shall provide real-time monitoring

### 4.5 Deployability

- **NFR-5.1**: The system shall be containerized using Docker
- **NFR-5.2**: The system shall use .NET Aspire for orchestration and service discovery
- **NFR-5.3**: The system shall follow a **local-first** deployment approach (run on developer machine)
- **NFR-5.4**: The system shall support cloud deployment as a future option (Azure Container Apps)

### 4.6 Usability

- **NFR-6.1**: The web client shall provide an intuitive interface for viewing market data
- **NFR-6.2**: The system shall provide clear status indicators for connection state
- **NFR-6.3**: The system shall provide visual feedback for price movements
- **NFR-6.4**: The web client shall be responsive and work on different screen sizes

### 4.7 Cost

- **NFR-7.1**: The system shall prioritize local deployment to minimize cloud costs
- **NFR-7.2**: The system shall use open-source components where possible (Redis, .NET, React)

## 5. Technical Specifications

### 5.1 Data Models

#### 5.1.1 Market Data Event (Simplified)

```csharp
public class MarketDataEvent
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Note**: For the minimal prototype, we focus on simple tick data (symbol, price, volume, timestamp). Order book and quote data can be added in future enhancements.
```

#### 5.1.2 Security (Price Cache Model)

```csharp
public class Security
{
    public string Symbol { get; set; }
    public decimal LastPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdate { get; set; }
    public decimal Change => LastPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
}
```
```

### 5.2 Key Interfaces

#### 5.2.1 Market Data Publisher

```csharp
public interface IMarketDataPublisher
{
    Task PublishAsync(MarketDataEvent marketDataEvent);
}
```

#### 5.2.2 Market Data Consumer

```csharp
public interface IMarketDataConsumer
{
    Task ProcessMarketDataAsync(MarketDataEvent marketDataEvent);
}
```
```

### 5.3 Communication Protocols

#### 5.3.1 Server-Sent Events (SSE)

- **Primary protocol** for Broker Service to Web Client communication
- Text-based protocol over HTTP
- One-way communication from server to client (push model)
- Automatic reconnection support built into EventSource API
- Lower overhead than WebSockets for one-way streaming
- Firewall and proxy friendly (standard HTTP)

**Example SSE Stream:**
```
event: marketdata
data: {"symbol":"NIFTY50","price":22435.50,"volume":1200,"timestamp":"2026-01-21T10:15:23.232Z"}

event: marketdata
data: {"symbol":"BANKNIFTY","price":48120.25,"volume":800,"timestamp":"2026-01-21T10:15:23.445Z"}

event: heartbeat
data: {"timestamp":"2026-01-21T10:15:30.000Z"}
```

#### 5.3.2 Redis Streams Protocol

- Event streaming backbone between Ingestion Service and Broker Service
- Provides persistent, ordered message queue
- Supports consumer groups for scalability
- Messages stored as key-value pairs

**Stream Entry Example:**
```
Stream: "marketdata:stream"
ID: "1674307523232-0"
Fields:
  - symbol: "NIFTY50"
  - price: "22435.50"
  - volume: "1200"
  - timestamp: "2026-01-21T10:15:23.232Z"
```

### 5.4 Data Storage & Caching

#### 5.4.1 In-Memory Storage (Redis)

- Latest market data cached in Redis for fast access
- Key pattern: `market:price:{symbol}`
- TTL: 1 hour (auto-expire stale data)
- Used by Broker Service for snapshot delivery to new clients

#### 5.4.2 Stream Storage (Redis Streams)

- Market data events stored in Redis Streams
- Retention: Configurable (default: last 1000 messages or 1 hour)
- Allows consumers to catch up on missed messages
- Provides durability for event replay if needed

#### 5.4.3 Persistent Storage (Future Enhancement)

- Time-series database (InfluxDB/TimescaleDB) for historical data
- SQL database for user preferences and configurations
- Apache Iceberg for unstructured financial data (quarterly reports, etc.)

## 6. Component Descriptions

### 6.1 Market Data Generator (Exchange Simulator)

**Purpose**: Simulates stock exchange tick generation.

**Technology**: .NET 10 Worker Service, BackgroundService

**Key Responsibilities**:
- Generate realistic price ticks for configured symbols
- Emit events every 200-500ms (configurable)
- Use random walk algorithm for price movements
- POST events to Market Ingestion Service HTTP endpoint

**Implementation Notes**:
- Runs as a background service
- Uses `Timer` or `PeriodicTimer` for tick generation
- Maintains base prices for each symbol
- Applies volatility factor for realistic movements

---

### 6.2 Market Ingestion Service

**Purpose**: Acts as the exchange feed handler (like NSE feed handler).

**Technology**: .NET 10 Minimal API

**Key Responsibilities**:
- Accept raw tick data via HTTP POST endpoint
- Validate data format (schema validation)
- Publish validated events to Redis Streams
- Emit OpenTelemetry traces

**API Endpoints**:
- `POST /api/marketdata` - Receive market tick data
- `GET /health` - Health check endpoint

**Implementation Notes**:
- Uses Aspire service discovery to locate Redis
- Implements input validation and error handling
- Buffers during spikes if needed
- Provides observability via OpenTelemetry

---

### 6.3 Event Stream Layer (Redis Streams)

**Purpose**: Event distribution backbone.

**Technology**: Redis Streams (via StackExchange.Redis)

**Key Responsibilities**:
- Persist incoming market data events
- Provide ordered, reliable message delivery
- Support multiple consumers (Broker Service, Analytics Engine)
- Enable event replay and catch-up

**Configuration**:
- Stream name: `marketdata:stream`
- Consumer group: `brokers` (for Broker Service)
- Retention: 1000 messages or 1 hour
- Hosted via Aspire orchestration

---

### 6.4 Broker Service

**Purpose**: Acts like Zerodha/IBKR backend - distributes market data to clients.

**Technology**: .NET Core API with SSE support

**Key Responsibilities**:
- Subscribe to Redis Streams as a consumer
- Maintain in-memory cache of latest prices (using Redis Cache)
- Serve web clients via Server-Sent Events (SSE)
- Apply throttling/rate-limiting logic
- Provide snapshot data on new connections
- Send periodic heartbeats

**API Endpoints**:
- `GET /api/stream/marketdata` - SSE endpoint for live market data
- `GET /api/prices` - Get snapshot of current prices
- `GET /health` - Health check

**Implementation Notes**:
- Uses channels for internal message distribution
- Implements connection management (track active clients)
- Applies backpressure if clients can't keep up
- Uses Redis for price caching
- Supports symbol-based filtering

---

### 6.5 Web UI (Trader View)

**Purpose**: Visualize real-time streaming data for end users.

**Technology**: ReactJS, TypeScript, Material UI, d3.js

**Key Features**:
- Connect to Broker Service via EventSource (SSE)
- Display live price updates in a table/grid
- Show price change animations (green/red indicators)
- Display candlestick chart with volume using d3.js
- Show connection status indicator
- Display latency metrics
- Implement auto-reconnection on disconnect
- Log important events

**Implementation Notes**:
- Uses EventSource API for SSE consumption
- State management via React hooks or context
- Real-time chart updates with d3.js
- Responsive design with Material UI
- Performance optimization for high-frequency updates

---

### 6.6 Analytics Engine (Future / Optional)

**Purpose**: Consume market data for aggregation and analysis.

**Technology**: .NET Worker Service

**Key Responsibilities**:
- Subscribe to Redis Streams as independent consumer
- Calculate market statistics (moving averages, volatility)
- Generate aggregated views
- Feed data to dashboards or trading algorithms

**Note**: This component is optional for the MVP but demonstrates multi-consumer pattern.

## 7. Data Flow Diagrams

### 7.1 Overall System Data Flow

```
┌─────────────────────┐
│ Market Data         │  Generates ticks every 200-500ms
│ Generator           │  
│ (Exchange Simulator)│
└──────────┬──────────┘
           │ HTTP POST
           │ /api/marketdata
           ▼
┌─────────────────────┐
│ Market Ingestion    │  Validates and publishes
│ Service             │  
│ (.NET Minimal API)  │
└──────────┬──────────┘
           │ XADD (publish)
           │ 
           ▼
┌─────────────────────┐
│ Redis Streams       │  Event backbone
│ (marketdata:stream) │  Ordered, persistent
└──────────┬──────────┘
           │
           │ XREAD (subscribe)
           │
           ▼
┌─────────────────────┐
│ Broker Service      │  Cache, throttle, broadcast
│ (SSE Server)        │  
└──────────┬──────────┘
           │ Server-Sent Events
           │ (EventSource)
           ▼
┌─────────────────────┐
│ Web UI              │  Display live prices
│ (React + d3.js)     │  
└─────────────────────┘
```

### 7.2 Detailed Event Flow

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Exchange   │────▶│  Ingestion   │────▶│    Redis     │────▶│    Broker    │
│  Simulator   │ (1) │   Service    │ (2) │   Streams    │ (3) │   Service    │
└──────────────┘     └──────────────┘     └──────────────┘     └──────┬───────┘
                                                                        │
                                                                        │ (4)
                                                                        ▼
                                                                 ┌──────────────┐
                                                                 │   Web UI     │
                                                                 │  (Browser)   │
                                                                 └──────────────┘

(1) HTTP POST with MarketDataEvent JSON
(2) Redis XADD to publish to stream
(3) Redis XREAD to consume from stream
(4) SSE push to browser (event: marketdata, data: {...})
```

### 7.3 Multi-Consumer Pattern (Future)

```
┌─────────────────────┐
│ Redis Streams       │
│ (marketdata:stream) │
└──────────┬──────────┘
           │
    ┌──────┴──────────────────┐
    │                         │
    │ Consumer Group          │ Consumer Group
    │ "brokers"               │ "analytics"
    │                         │
    ▼                         ▼
┌─────────────┐      ┌─────────────────┐
│ Broker      │      │ Analytics       │
│ Service     │      │ Engine          │
└─────────────┘      └─────────────────┘
```

**Note**: The multi-consumer pattern demonstrates Redis Streams' ability to fanout to multiple independent consumers without coordination.

## 8. Implementation Requirements

### 8.1 Development Environment

- .NET 10 SDK (or latest .NET 9 preview)
- .NET Aspire 9.5 workload (`dotnet workload install aspire`)
- Visual Studio 2022 (17.10+) or Visual Studio Code with C# Dev Kit
- Docker Desktop (for Redis container)
- Git for version control
- Node.js and npm (for React frontend)

### 8.2 Aspire-Based Project Structure

```
stock-market-simulator/
├── src/
│   ├── MarketSimulator/                    # Exchange Simulator (Worker Service)
│   │   ├── MarketSimulator.csproj
│   │   ├── Program.cs
│   │   ├── ExchangeSimulator.cs           # BackgroundService
│   │   └── Services/
│   │       └── TickGenerator.cs
│   │
│   ├── MarketIngestion.Api/               # Ingestion Service (Minimal API)
│   │   ├── MarketIngestion.Api.csproj
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   └── MarketDataEndpoints.cs
│   │   └── Services/
│   │       └── RedisStreamPublisher.cs
│   │
│   ├── Broker.Service/                    # Broker Service (API with SSE)
│   │   ├── Broker.Service.csproj
│   │   ├── Program.cs
│   │   ├── Services/
│   │   │   ├── StreamConsumerService.cs   # Redis consumer
│   │   │   ├── PriceCacheService.cs       # Redis cache
│   │   │   └── SseService.cs              # SSE broadcasting
│   │   └── Endpoints/
│   │       ├── StreamEndpoints.cs         # SSE endpoint
│   │       └── PriceEndpoints.cs          # Snapshot API
│   │
│   ├── WebApp/                            # Web UI (React)
│   │   ├── WebApp.csproj                  # ASP.NET Core host
│   │   ├── Program.cs
│   │   └── ClientApp/                     # React app
│   │       ├── package.json
│   │       ├── src/
│   │       │   ├── App.tsx
│   │       │   ├── components/
│   │       │   │   ├── PriceGrid.tsx
│   │       │   │   ├── CandlestickChart.tsx
│   │       │   │   └── ConnectionStatus.tsx
│   │       │   └── services/
│   │       │       └── marketDataService.ts  # SSE client
│   │       └── public/
│   │
│   ├── Shared.Contracts/                  # Shared models/DTOs
│   │   ├── Shared.Contracts.csproj
│   │   └── Models/
│   │       ├── MarketDataEvent.cs
│   │       └── Security.cs
│   │
│   └── AppHost/                           # Aspire AppHost (Orchestration)
│       ├── AppHost.csproj
│       ├── Program.cs                     # Service composition
│       └── appsettings.json
│
├── tests/
│   ├── MarketSimulator.Tests/
│   ├── MarketIngestion.Api.Tests/
│   └── Broker.Service.Tests/
│
├── docs/
│   └── requirements/
│       ├── objective.md
│       └── prd.md
│
├── .gitignore
├── README.md
└── StockMarketSimulator.sln              # Solution file
```

### 8.3 Aspire AppHost Configuration

The AppHost project orchestrates all services and dependencies:

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis as a dependency
var redis = builder.AddRedis("redis")
    .WithRedisCommander();  // Optional: Redis UI

// Add backend services
var ingestion = builder.AddProject<Projects.MarketIngestion_Api>("ingestion")
    .WithReference(redis);

var broker = builder.AddProject<Projects.Broker_Service>("broker")
    .WithReference(redis);

// Add simulator (generates data)
builder.AddProject<Projects.MarketSimulator>("simulator")
    .WithReference(ingestion);

// Add web UI
builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(broker);

builder.Build().Run();
```

**Benefits of Aspire Orchestration**:
- Automatic service discovery
- Built-in OpenTelemetry observability
- Redis container management
- Unified dashboard for all services
- Environment-based configuration

### 8.4 Implementation Steps (Phase-wise)

#### Phase 1: Foundation & Core Flow

1. **Project Setup**
   - Create Aspire AppHost solution
   - Add Shared.Contracts project with data models
   - Configure Redis dependency in AppHost
   - Set up OpenTelemetry integration

2. **Exchange Simulator**
   - Create Worker Service project
   - Implement TickGenerator with random walk algorithm
   - Configure tick generation interval (200-500ms)
   - Add HTTP client to POST to Ingestion API

3. **Market Ingestion Service**
   - Create Minimal API project
   - Implement POST /api/marketdata endpoint
   - Add input validation
   - Integrate Redis Streams publisher (XADD)
   - Configure service discovery

4. **Broker Service**
   - Create API project
   - Implement Redis Streams consumer (XREAD)
   - Add price cache service (Redis)
   - Implement SSE endpoint (GET /api/stream/marketdata)
   - Add snapshot endpoint (GET /api/prices)

5. **Basic Web UI**
   - Create React app
   - Implement EventSource client for SSE
   - Display live prices in a simple table
   - Add connection status indicator

6. **Integration Testing**
   - Test end-to-end data flow
   - Verify latency < 300ms
   - Test reconnection scenarios

#### Phase 2: Polish & Features

7. **Enhanced Web UI**
   - Implement candlestick chart with d3.js
   - Add volume display
   - Implement price change animations (green/red)
   - Add latency metrics display
   - Improve responsive design

8. **Observability**
   - Add custom metrics (throughput, connection count)
   - Configure trace sampling
   - Add structured logging
   - Use Aspire dashboard for monitoring

9. **Resilience**
   - Add retry policies (Polly)
   - Implement circuit breakers
   - Handle Redis unavailability gracefully
   - Add health checks

#### Phase 3: Documentation & Showcase

10. **Documentation**
    - Update README with setup instructions
    - Add architecture diagrams
    - Document API endpoints
    - Create demo video/screenshots

11. **Performance Testing**
    - Test with 10k+ concurrent connections (simulated)
    - Measure latency percentiles (p50, p95, p99)
    - Optimize bottlenecks

12. **Deployment**
    - Create Docker compose file (alternative to Aspire)
    - Test deployment on local machine
    - Document cloud deployment options

### 8.5 Testing Strategy

#### 8.5.1 Unit Testing

- **Exchange Simulator**: Test tick generation logic, random walk algorithm
- **Ingestion Service**: Test validation logic, error handling
- **Broker Service**: Test caching logic, SSE message formatting
- **Shared Models**: Test data serialization/deserialization

**Framework**: xUnit, FluentAssertions

#### 8.5.2 Integration Testing

- **End-to-End Flow**: Verify data flows from simulator to web UI
- **Redis Integration**: Test publish/subscribe with actual Redis
- **API Integration**: Test HTTP endpoints with WebApplicationFactory
- **SSE Integration**: Test EventSource connection and message delivery

**Framework**: xUnit, Testcontainers (for Redis), WebApplicationFactory

#### 8.5.3 Performance Testing

- **Latency Testing**: Measure end-to-end latency with timestamps
  - Target: < 300ms (p95)
  - Tool: Custom benchmark with BenchmarkDotNet

- **Throughput Testing**: Measure messages per second
  - Target: 10+ events/sec per symbol
  - Tool: Load testing with k6 or NBomber

- **Scalability Testing**: Test with simulated concurrent users
  - Target: 10k+ concurrent connections
  - Tool: WebSocket/SSE load testing tool

#### 8.5.4 User Acceptance Testing

- **Web UI Functionality**: Verify all UI features work correctly
- **Connection Handling**: Test disconnect/reconnect scenarios
- **Data Accuracy**: Verify prices match source data
- **Visual Polish**: Ensure animations and indicators work

**Approach**: Manual testing, screenshots, demo video

## 9. Deployment Requirements

### 9.1 Local Development Deployment (Primary)

**Approach**: Run via .NET Aspire AppHost

**Prerequisites**:
- .NET 10 SDK
- Docker Desktop (for Redis)
- Node.js (for React build)

**Steps**:
1. Clone repository
2. Run `dotnet restore` in solution root
3. Run `dotnet run --project src/AppHost`
4. Aspire dashboard opens automatically
5. Access web UI from dashboard links

**Benefits**:
- One-command startup
- Automatic service discovery
- Built-in observability dashboard
- **Local-first, zero cloud cost**

### 9.2 System Requirements

**Minimum**:
- CPU: 2 cores
- RAM: 4GB
- Storage: 5GB
- Network: Local (no external requirements)

**Recommended**:
- CPU: 4 cores
- RAM: 8GB
- Storage: 10GB
- Network: 100Mbps (for testing high concurrency)

### 9.3 Containerized Deployment (Alternative)

**Approach**: Docker Compose (without Aspire)

```yaml
# docker-compose.yml
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
  
  ingestion:
    build: ./src/MarketIngestion.Api
    environment:
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis
  
  broker:
    build: ./src/Broker.Service
    ports:
      - "5001:8080"
    depends_on:
      - redis
  
  simulator:
    build: ./src/MarketSimulator
    depends_on:
      - ingestion
  
  webapp:
    build: ./src/WebApp
    ports:
      - "5000:8080"
    depends_on:
      - broker
```

**Usage**: `docker compose up`

### 9.4 Cloud Deployment (Future)

**Option 1: Azure Container Apps**
- Deploy each service as a container app
- Use Azure Redis Cache
- Leverage Aspire deployment extensions
- Estimated cost: $50-100/month (can be shut down when not in use)

**Option 2: AWS ECS**
- Deploy services to ECS Fargate
- Use Amazon ElastiCache for Redis
- Configure Application Load Balancer
- Similar cost structure

**Note**: Cloud deployment is optional. The primary goal is local demonstration.

## 10. Future Enhancements

### 10.1 Multiple Broker Types (Moved from MVP)

Implement specialized broker services to demonstrate different consumption patterns:

- **Retail Broker**: Basic functionality, rate-limited updates
- **Institutional Broker**: Higher throughput, batch updates, analytics
- **Proprietary Trading Desk**: Full-depth order book, sub-millisecond latency

**Benefit**: Demonstrates fan-out architecture with different consumer requirements

### 10.2 Advanced Market Data

- **Order Book Simulation**: Full depth-of-market data (bid/ask levels)
- **Quote Data**: Separate bid/ask price feeds
- **Market Statistics**: VWAP, moving averages, volatility calculations
- **Market Replay Mode**: Replay historical data for testing

### 10.3 Additional Client Applications

- **Mobile Application**: Native iOS/Android with WebSocket
- **Desktop Application**: WPF or Electron with advanced charting
- **Trading Algorithm API**: REST/gRPC API for algo trading bots
- **Excel Add-in**: Real-time data in Excel via RTD server

### 10.4 Advanced Communication Protocols

- **WebSocket Support**: Bidirectional communication for mobile
- **gRPC Streaming**: For high-performance institutional clients
- **FIX Protocol Simulation**: Industry-standard financial protocol
- **SSE vs WebSocket Comparison**: Side-by-side performance demo

### 10.5 Data Storage & Analytics

- **Time-Series Database**: InfluxDB or TimescaleDB for historical data
- **Data Lake**: Apache Iceberg for long-term storage
- **Analytics Pipeline**: Spark-based processing for quarterly reports
- **Financial Fundamentals Viewer**: Display company financial data

### 10.6 Performance Optimizations

- **FPGA Acceleration**: Ultra-low latency (sub-microsecond)
- **Kernel Bypass**: DPDK or similar for network optimization
- **Binary Protocols**: MessagePack, Protobuf for reduced overhead
- **Data Compression**: Snappy or LZ4 for bandwidth optimization

### 10.7 Real Data Integration

- **Exchange Feed Adapters**: Connect to real exchange APIs
- **Market Data Vendors**: Integrate Bloomberg, Reuters, etc.
- **Multi-Asset Support**: Forex, crypto, commodities, derivatives
- **Real-Time News Integration**: Financial news feeds

### 10.8 Advanced Features

- **Authentication & Authorization**: User management, role-based access
- **Symbol Watchlists**: Save user preferences
- **Price Alerts**: Notify users when price crosses threshold
- **Market Data Entitlements**: Control access to specific data feeds
- **Latency Heatmap**: Visualize latency across components
- **Performance Dashboard**: Real-time system health metrics

### 10.9 Alternative Streaming Technologies

Compare and contrast different streaming approaches:

- **Apache Kafka**: Industry standard, high throughput
- **Azure Event Hubs**: Managed Kafka-compatible service
- **RabbitMQ Streams**: Alternative message broker
- **NATS JetStream**: Lightweight, high-performance streaming

## 11. Success Criteria & Acceptance

### 11.1 Technical Success Criteria

- ✅ End-to-end data flow working (Exchange → Ingestion → Stream → Broker → Web UI)
- ✅ Latency < 300ms (p95) measured with timestamps
- ✅ System handles 10k+ simulated concurrent connections
- ✅ All services run via single Aspire command
- ✅ OpenTelemetry traces visible in Aspire dashboard
- ✅ Redis Streams successfully fanout to consumers
- ✅ SSE connection maintains stability for 5+ minutes
- ✅ Web UI displays real-time price updates with animations
- ✅ Candlestick chart renders with volume data

### 11.2 Demonstration Success Criteria

- ✅ README explains architecture clearly
- ✅ Setup instructions enable running in < 5 minutes
- ✅ Screenshots/video demonstrate live streaming
- ✅ Architecture diagrams match implementation
- ✅ Code is well-structured and documented
- ✅ Demonstrates event-driven patterns effectively

### 11.3 Alignment with Objective

This PRD ensures the implementation will demonstrate:

- ✅ How real-time stock market data flows
- ✅ How streaming systems are designed
- ✅ How brokers distribute live prices
- ✅ How modern .NET + Aspire model real-world event-driven systems

This is **exactly** the kind of project that:
- Internal reviewers respect
- Architects relate to
- Hiring managers remember

---

## 12. Conclusion

This Product Requirements Document provides comprehensive, engineering-ready specifications for building a **minimal but realistic** Stock Market Data Streaming System. The implementation uses .NET 10 and .NET Aspire to create a modern, scalable, and observable system that accurately demonstrates real-world market data architectures.

### Key Deliverables

1. **Working System**: 
   - Market Data Generator (Exchange Simulator)
   - Market Ingestion Service (API)
   - Redis Streams (Event Backbone)
   - Broker Service (SSE Server)
   - Web UI (React + d3.js)

2. **Observability**: OpenTelemetry traces, metrics, and Aspire dashboard

3. **Documentation**: Architecture diagrams, setup guide, API docs

4. **Portfolio Value**: Demonstrates technical depth in:
   - Real-time systems
   - Event-driven architecture
   - Streaming technologies
   - Modern .NET + Aspire
   - Financial technology domain

### Relationship to Objective

This PRD is the engineering implementation specification for the vision outlined in [objective.md](./objective.md). While the objective describes **what** we're building and **why**, this PRD describes **how** to build it with specific technical requirements, data models, APIs, and implementation steps.

**Engineering teams should use this PRD as the primary reference** for implementation, referring back to objective.md for context on the overall goals and vision.

---

**Document Version**: 1.0  
**Last Updated**: January 2026  
**Next Review**: After Phase 1 implementation  
**Owner**: Raj Champaneriya