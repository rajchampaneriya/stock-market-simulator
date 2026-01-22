# C4 Model Diagrams
## Stock Market Simulator

This document contains C4 model diagrams in Mermaid format for the Stock Market Simulator system.

---

## Level 1: System Context Diagram

Shows the Stock Market Simulator system and its relationship with users.

```mermaid
flowchart TB
    subgraph External_Users["External Users"]
        Trader((Trader))
    end

    subgraph Stock_Market_Simulator["Stock Market Simulator System"]
        System[Stock Market Simulator<br/>Real-time market data<br/>streaming system]
    end

    Trader -->|Views real-time<br/>stock prices| System
```

**Description**: The Trader interacts with the Stock Market Simulator to view real-time stock price updates. This is a demonstration system that generates synthetic market data.

---

## Level 2: Container Diagram

Shows the high-level technology choices and how containers communicate.

```mermaid
flowchart TB
    subgraph User["User"]
        Trader((Trader))
    end

    subgraph StockMarketSimulator["Stock Market Simulator"]
        WebApp[Web Application<br/>React TypeScript<br/>Displays real-time<br/>stock prices]
        
        BrokerService[Broker Service<br/>dotNET 10 API<br/>Streams data<br/>via SSE]
        
        IngestionAPI[Market Ingestion API<br/>dotNET 10 Minimal API<br/>Receives and validates<br/>market data]
        
        MarketSimulator[Market Simulator<br/>dotNET 10 Worker<br/>Generates synthetic<br/>market data]
        
        RedisDB[(Redis<br/>Redis 7<br/>Message broker<br/>with Streams)]
    end

    Trader -->|HTTPS| WebApp
    WebApp -->|SSE| BrokerService
    BrokerService -->|XREADGROUP| RedisDB
    IngestionAPI -->|XADD| RedisDB
    MarketSimulator -->|HTTP POST| IngestionAPI
```

**Container Descriptions**:

| Container | Technology | Responsibility |
|-----------|-----------|---------------|
| Web Application | React, TypeScript, Material UI | User interface for viewing prices |
| Broker Service | .NET 10, ASP.NET Core | Consumes from Redis, streams via SSE |
| Market Ingestion API | .NET 10 Minimal API | Validates and publishes market data |
| Market Simulator | .NET 10 Worker Service | Generates synthetic price ticks |
| Redis | Redis 7 with Streams | Message broker and event store |

---

## Level 3: Component Diagrams

### 3.1 Market Simulator Components

```mermaid
flowchart TB
    subgraph MarketSimulatorService["Market Simulator Service"]
        Program[Program.cs<br/>Application Entry]
        ExchangeSimulator[ExchangeSimulator<br/>BackgroundService<br/>Generates price ticks]
        HttpClientFactory[HttpClientFactory<br/>HTTP connection pool]
        Configuration[Configuration<br/>Symbols Volatility<br/>TickInterval]
    end

    Program --> ExchangeSimulator
    ExchangeSimulator --> HttpClientFactory
    ExchangeSimulator --> Configuration
    HttpClientFactory -->|HTTP POST| IngestionAPI[Ingestion API]
```

**Component Descriptions**:

| Component | Type | Responsibility |
|-----------|------|---------------|
| Program.cs | Entry Point | Configures DI and starts host |
| ExchangeSimulator | BackgroundService | Generates ticks using random walk |
| HttpClientFactory | Factory | Manages HTTP connections |
| Configuration | IConfiguration | Provides runtime settings |

---

### 3.2 Market Ingestion API Components

```mermaid
flowchart TB
    subgraph IngestionAPIService["Market Ingestion API"]
        Program[Program.cs<br/>Minimal API<br/>Entry Point]
        MarketDataEndpoint[POST /api/marketdata<br/>Receives market data]
        HealthEndpoint[GET /health<br/>Health check]
        Validation[ValidateMarketData<br/>Input validation]
        RedisPublisher[Redis Publisher<br/>XADD to stream]
    end

    subgraph External["External"]
        Redis[(Redis Streams)]
        Simulator[Market Simulator]
    end

    Simulator -->|POST| MarketDataEndpoint
    MarketDataEndpoint --> Validation
    Validation -->|Valid| RedisPublisher
    Validation -->|Invalid| Error[400 Response]
    RedisPublisher -->|XADD| Redis
    HealthEndpoint -->|Check| Redis
```

**Component Descriptions**:

| Component | Type | Responsibility |
|-----------|------|---------------|
| Program.cs | Minimal API | Defines routes and middleware |
| POST /api/marketdata | Endpoint | Receives and processes market data |
| GET /health | Endpoint | Reports service health |
| ValidateMarketData | Function | Validates input data |
| Redis Publisher | Service | Publishes to Redis Streams |

---

### 3.3 Broker Service Components

```mermaid
flowchart TB
    subgraph BrokerServiceContainer["Broker Service"]
        Program[Program.cs<br/>Application Entry]
        RedisConsumer[RedisStreamConsumer<br/>BackgroundService<br/>Consumes from Redis]
        SseController[SseController<br/>API Controller<br/>SSE streaming]
        Channel[Channel of MarketDataEvent<br/>Internal broadcast]
        SecurityCache[Security Cache<br/>Price tracking]
    end

    subgraph External["External"]
        Redis[(Redis Streams)]
        Browser[Web Browser]
    end

    Program --> RedisConsumer
    Program --> SseController
    Redis -->|XREADGROUP| RedisConsumer
    RedisConsumer --> SecurityCache
    RedisConsumer --> Channel
    Channel --> SseController
    SseController -->|SSE| Browser
```

**Component Descriptions**:

| Component | Type | Responsibility |
|-----------|------|---------------|
| Program.cs | Entry Point | Configures services and middleware |
| RedisStreamConsumer | BackgroundService | Consumes messages from Redis |
| SseController | ApiController | Handles SSE client connections |
| Channel | Channel&lt;T&gt; | Broadcast buffer for SSE |
| Security Cache | Dictionary | Tracks latest prices |

---

### 3.4 Web Application Components

```mermaid
flowchart TB
    subgraph WebAppContainer["Web Application"]
        AppComponent[App.tsx<br/>Main Component]
        UseMarketData[useMarketData<br/>Custom Hook<br/>SSE connection]
        StockCard[StockCard<br/>Component<br/>Price display]
        CandlestickChart[CandlestickChart<br/>Component<br/>D3 visualization]
        PriceAggregator[PriceAggregator<br/>Utility<br/>OHLC calculation]
    end

    subgraph External["External"]
        EventSourceAPI[EventSource API<br/>Browser SSE]
        D3js[D3.js<br/>Charting]
        MaterialUI[Material UI<br/>Components]
    end

    AppComponent --> UseMarketData
    AppComponent --> StockCard
    UseMarketData --> EventSourceAPI
    UseMarketData --> PriceAggregator
    StockCard --> CandlestickChart
    StockCard --> MaterialUI
    CandlestickChart --> D3js
```

**Component Descriptions**:

| Component | Type | Responsibility |
|-----------|------|---------------|
| App.tsx | React Component | Main application layout |
| useMarketData | Custom Hook | Manages SSE connection and state |
| StockCard | React Component | Displays individual security |
| CandlestickChart | React Component | Renders D3 candlestick chart |
| PriceAggregator | Utility Class | Aggregates ticks into OHLC |

---

## Data Flow Diagram

```mermaid
flowchart LR
    subgraph Generation["1 Data Generation"]
        MS[Market Simulator]
    end

    subgraph Ingestion["2 Data Ingestion"]
        API[Ingestion API]
    end

    subgraph Storage["3 Event Storage"]
        Redis[(Redis Streams)]
    end

    subgraph Distribution["4 Data Distribution"]
        Broker[Broker Service]
    end

    subgraph Presentation["5 Presentation"]
        Web[Web Application]
    end

    MS -->|HTTP POST<br/>MarketDataEvent| API
    API -->|XADD<br/>JSON message| Redis
    Redis -->|XREADGROUP<br/>Consumer group| Broker
    Broker -->|SSE<br/>Event stream| Web
```

---

## Deployment Diagram

```mermaid
flowchart TB
    subgraph DeveloperMachine["Developer Machine"]
        subgraph DockerContainer["Docker Container"]
            Redis[(Redis 7)]
        end
        
        subgraph DotNetProcesses["dotNET Runtime"]
            MarketSim[Market Simulator<br/>Port N/A]
            IngestionAPI[Ingestion API<br/>Port 5001]
            BrokerService[Broker Service<br/>Port 5002]
        end
        
        subgraph NodeJS["Node.js Runtime"]
            WebApp[Web Application<br/>Port 5000]
        end
    end

    subgraph Browser["Web Browser"]
        UserInterface[Trader View]
    end

    MarketSim -->|localhost:5001| IngestionAPI
    IngestionAPI -->|localhost:6379| Redis
    Redis -->|localhost:6379| BrokerService
    BrokerService -->|SSE| WebApp
    WebApp --> UserInterface
```

---

## Sequence Diagram: Complete Data Flow

```mermaid
sequenceDiagram
    participant MS as Market Simulator
    participant API as Ingestion API
    participant Redis as Redis Streams
    participant Broker as Broker Service
    participant Web as Web Application
    participant User as Trader

    Note over MS: Timer tick every 300ms

    loop Every tick interval
        MS->>MS: GenerateTick using random walk
        MS->>API: POST /api/marketdata
        
        activate API
        API->>API: ValidateMarketData
        API->>Redis: XADD marketdata stream
        Redis-->>API: Message ID
        API-->>MS: 200 OK
        deactivate API
    end

    Note over Broker: Background consumer

    loop Continuous consumption
        Broker->>Redis: XREADGROUP brokers broker_1
        Redis-->>Broker: Stream entries
        Broker->>Broker: UpdateCache
        Broker->>Broker: Channel WriteAsync
        Broker->>Redis: XACK
    end

    Note over Web: SSE connection active

    loop For each message in channel
        Broker->>Web: SSE event marketdata
        Web->>Web: Update React state
        Web->>User: Render updated price
    end
```

---

## Error Handling Flow

```mermaid
flowchart TB
    subgraph SimulatorErrors["Market Simulator Errors"]
        SE1[HTTP Error] --> SE2[Retry with backoff]
        SE2 --> SE3{Max retries?}
        SE3 -->|No| SE1
        SE3 -->|Yes| SE4[Log and skip]
    end

    subgraph IngestionErrors["Ingestion API Errors"]
        IE1[Validation Error] --> IE2[Return 400]
        IE3[Redis Connection] --> IE4[Return 503]
        IE5[Redis Timeout] --> IE6[Return 504]
    end

    subgraph BrokerErrors["Broker Service Errors"]
        BE1[Redis Connection] --> BE2[Wait 5s and retry]
        BE3[Redis Timeout] --> BE4[Wait 2s and retry]
        BE5[JSON Error] --> BE6[ACK and skip]
        BE7[Fatal Error] --> BE8[Wait 10s and restart]
    end

    subgraph WebAppErrors["Web Application Errors"]
        WE1[SSE Error] --> WE2{Connection closed?}
        WE2 -->|Yes| WE3{Max attempts?}
        WE3 -->|No| WE4[Reconnect]
        WE3 -->|Yes| WE5[Show error]
        WE2 -->|No| WE6[Show warning]
    end
```

---

## References

- [C4 Model](https://c4model.com/)
- [Mermaid Diagrams](https://mermaid.js.org/)
- [High-Level Design](./HIGH_LEVEL_DESIGN.md)
- [Low-Level Design](./LOW_LEVEL_DESIGN.md)
