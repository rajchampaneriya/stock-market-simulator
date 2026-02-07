# C4 Architecture Diagrams — Stock Market Simulator

> **Format:** Mermaid.js  
> **Model:** C4 (Context, Container, Component, Code)  
> **Last Updated:** 2026-02-07

---

## Level 1: System Context Diagram

Shows the Stock Market Simulator system in its environment — who uses it and what external systems it interacts with.

```mermaid
graph TB
    Trader((Trader or Viewer))
    SMS[Stock Market Simulator System]
    Redis[(Redis Server)]
    OTel[OpenTelemetry Collector]

    Trader -->|Views real time market data via browser| SMS
    SMS -->|Publishes and consumes market data streams| Redis
    SMS -->|Exports distributed traces via OTLP| OTel

    style Trader fill:#08427b,color:#fff,stroke:#08427b
    style SMS fill:#1168bd,color:#fff,stroke:#0b4884
    style Redis fill:#999999,color:#fff,stroke:#666666
    style OTel fill:#999999,color:#fff,stroke:#666666
```

**Key relationships:**
- A **Trader or Viewer** interacts with the system through a web browser to observe real-time stock prices and candlestick charts.
- The system uses **Redis** as its internal message broker (append-only stream).
- The system exports **OpenTelemetry** traces for observability and debugging.

---

## Level 2: Container Diagram

Zooms into the system to show the major deployable units (containers) and how they communicate.

```mermaid
graph TB
    Trader((Trader))

    subgraph Stock_Market_Simulator_System
        AppHost[Aspire AppHost<br/>Orchestrator]
        MarketSim[Market Simulator<br/>dotNET Worker Service]
        IngestionAPI[Market Ingestion API<br/>ASP_NET Minimal API]
        BrokerSvc[Broker Service<br/>ASP_NET Web API]
        Frontend[Web Frontend<br/>React TypeScript Vite]
    end

    Redis[(Redis<br/>Stream Storage)]

    MarketSim -->|HTTP POST /api/marketdata<br/>MarketDataEvent JSON| IngestionAPI
    IngestionAPI -->|XADD marketdata:stream<br/>Append to stream| Redis
    Redis -->|XREADGROUP<br/>Consumer group polling| BrokerSvc
    BrokerSvc -->|SSE text/event-stream<br/>Ticks and Candles| Frontend
    Frontend -->|HTTP in browser| Trader

    AppHost -.->|Orchestrates lifecycle and config| MarketSim
    AppHost -.->|Orchestrates lifecycle and config| IngestionAPI
    AppHost -.->|Orchestrates lifecycle and config| BrokerSvc
    AppHost -.->|Orchestrates lifecycle and config| Frontend
    AppHost -.->|Provisions Redis container| Redis

    style Trader fill:#08427b,color:#fff
    style AppHost fill:#438dd5,color:#fff
    style MarketSim fill:#438dd5,color:#fff
    style IngestionAPI fill:#438dd5,color:#fff
    style BrokerSvc fill:#438dd5,color:#fff
    style Frontend fill:#438dd5,color:#fff
    style Redis fill:#999999,color:#fff
```

**Container Descriptions:**

| Container | Technology | Responsibility |
|---|---|---|
| **Aspire AppHost** | .NET Aspire 13.1 | Orchestrates startup order, injects connection strings, manages health checks |
| **Market Simulator** | .NET 10 Worker Service | Generates realistic price ticks using GBM, Ornstein-Uhlenbeck, and GARCH models |
| **Market Ingestion API** | ASP.NET Core 10 Minimal API | Validates incoming market data and publishes to Redis Stream |
| **Broker Service** | ASP.NET Core 10 Web API | Consumes from Redis, aggregates candlesticks, streams via SSE |
| **Web Frontend** | React 19 + TypeScript 5.9 + D3.js 7 | Displays real-time dashboard with candlestick charts and watchlist |
| **Redis** | Redis 7+ (Docker) | Append-only stream storage with consumer group support |

---

## Level 3: Component Diagram — Broker Service

The Broker Service is the most complex container. This diagram shows its internal components.

```mermaid
graph TB
    subgraph Broker_Service
        RSC[RedisStreamConsumer<br/>BackgroundService]
        TB[TickBroadcaster<br/>Fan out Writer]
        SSEChan[SSE Tick Channel<br/>Bounded 1000]
        AggChan[Aggregator Tick Channel<br/>Bounded 2000]
        CA[CandlestickAggregator<br/>BackgroundService]
        CH[CandlestickHistory<br/>Ring Buffer per Symbol]
        CandleChan[Candle Channel<br/>Bounded 500]
        MEB[MarketEventBuffer<br/>Replay Ring 1000]

        SseCtrl[SseController<br/>Primary SSE Endpoint]
        SseCtrlV2[SseControllerV2<br/>SSE with Replay]
        MinAPI[Minimal API SSE<br/>Demo Endpoint]
        RedisCtrl[RedisMonitoringController<br/>Stream Diagnostics]
        SecCache[Security Cache<br/>In Memory Dictionary]
    end

    Redis[(Redis Stream)] -->|XREADGROUP| RSC
    RSC --> SecCache
    RSC --> TB
    TB --> SSEChan
    TB --> AggChan
    AggChan --> CA
    CA --> CandleChan
    CA --> CH

    SSEChan --> SseCtrl
    CandleChan --> SseCtrl
    CH -->|Replay history on connect| SseCtrl

    SSEChan --> SseCtrlV2
    MEB --> SseCtrlV2

    SSEChan --> MinAPI

    Redis -->|XINFO| RedisCtrl

    SseCtrl -->|SSE stream| Browser((Browser))
    SseCtrlV2 -->|SSE stream| Browser
    MinAPI -->|SSE stream| Browser
    RedisCtrl -->|JSON response| Browser

    style RSC fill:#1168bd,color:#fff
    style TB fill:#1168bd,color:#fff
    style CA fill:#1168bd,color:#fff
    style CH fill:#1168bd,color:#fff
    style SseCtrl fill:#438dd5,color:#fff
    style SseCtrlV2 fill:#438dd5,color:#fff
    style MinAPI fill:#438dd5,color:#fff
    style RedisCtrl fill:#438dd5,color:#fff
    style SSEChan fill:#85bbf0,color:#000
    style AggChan fill:#85bbf0,color:#000
    style CandleChan fill:#85bbf0,color:#000
    style MEB fill:#85bbf0,color:#000
    style SecCache fill:#85bbf0,color:#000
```

**Component Descriptions:**

| Component | Type | Purpose |
|---|---|---|
| `RedisStreamConsumer` | `BackgroundService` | Polls Redis stream via consumer group, deserialises events, updates security cache, pushes to broadcaster |
| `TickBroadcaster` | Singleton | Fans out each tick to both the SSE channel and the aggregator channel via non-blocking writes |
| `CandlestickAggregator` | `BackgroundService` | Reads ticks from aggregator channel, builds 1-minute OHLCV candles, emits throttled updates |
| `CandlestickHistory` | Singleton | Ring buffer of completed candles per symbol; seeds historical candles on first access |
| `MarketEventBuffer` | Singleton | Ring buffer of SSE events (1000 max) supporting `Last-Event-ID` replay |
| `SseController` | API Controller | Primary SSE endpoint; multiplexes ticks + candles; sends history on connect; heartbeats |
| `SseControllerV2` | API Controller | SSE with `Last-Event-ID` replay support for reconnection resilience |
| `RedisMonitoringController` | API Controller | Exposes Redis stream and consumer group metrics |
| Security Cache | Dictionary | In-memory per-symbol price tracking (last price, previous price, volume) |

---

## Level 3: Component Diagram — Market Simulator

```mermaid
graph TB
    subgraph Market_Simulator
        Prog[Program.cs<br/>Host Builder]
        ES[ExchangeSimulator<br/>BackgroundService]
        RPE[RealisticPriceEngine<br/>Pricing Model]
        HTTP[HttpClient Factory<br/>IngestionApi Client]
        OTel[OpenTelemetry<br/>Tracing]
    end

    Prog --> ES
    ES --> RPE
    ES --> HTTP
    Prog --> OTel

    HTTP -->|POST /api/marketdata| IngestionAPI((Ingestion API))
    OTel -->|OTLP traces| Collector((Collector))

    style ES fill:#1168bd,color:#fff
    style RPE fill:#1168bd,color:#fff
    style HTTP fill:#438dd5,color:#fff
    style OTel fill:#438dd5,color:#fff
```

---

## Level 3: Component Diagram — Web Frontend

```mermaid
graph TB
    subgraph React_Frontend
        AppComp[App Component<br/>Router Setup]
        Nav[Navigation<br/>Header and Clock]
        Dash[Dashboard Page<br/>Main Trading View]
        RedisMon[Redis Monitoring Page<br/>Stream Diagnostics]

        Hook[useMarketData Hook<br/>SSE Connection Manager]
        Agg[PriceAggregator<br/>Candle History Buffer]

        Chart[CandlestickChart<br/>D3 SVG Renderer]
        Card[StockCard<br/>Watchlist Row]
        StreamTable[RedisStreamTable<br/>Data Table]
        Explain[RedisExplanation<br/>Educational Content]

        Service[RedisMonitoringService<br/>HTTP Client]
    end

    AppComp --> Nav
    AppComp --> Dash
    AppComp --> RedisMon

    Dash --> Hook
    Hook --> Agg
    Dash --> Chart
    Dash --> Card

    RedisMon --> Service
    RedisMon --> StreamTable
    RedisMon --> Explain

    Hook -->|EventSource API| BrokerSSE((Broker SSE Endpoint))
    Service -->|fetch API| BrokerREST((Broker REST Endpoint))

    style AppComp fill:#438dd5,color:#fff
    style Dash fill:#1168bd,color:#fff
    style RedisMon fill:#1168bd,color:#fff
    style Hook fill:#85bbf0,color:#000
    style Agg fill:#85bbf0,color:#000
    style Chart fill:#85bbf0,color:#000
    style Card fill:#85bbf0,color:#000
```

---

## Data Flow Diagram — End-to-End

```mermaid
sequenceDiagram
    participant Sim as Market Simulator
    participant RPE as RealisticPriceEngine
    participant API as Ingestion API
    participant Redis as Redis Stream
    participant RSC as RedisStreamConsumer
    participant TB as TickBroadcaster
    participant SSE as SseController
    participant CA as CandlestickAggregator
    participant CH as CandlestickHistory
    participant FE as React Frontend

    Note over Sim,FE: Every 300ms per symbol
    Sim->>RPE: NextTick(NIFTY50)
    RPE-->>Sim: price=1002.35, volume=5420
    Sim->>API: POST /api/marketdata
    API->>API: Validate input
    API->>Redis: XADD marketdata:stream
    Redis-->>API: messageId

    RSC->>Redis: XREADGROUP GROUP brokers
    Redis-->>RSC: StreamEntry
    RSC->>RSC: Deserialize and update cache
    RSC->>TB: Broadcast(event)
    TB->>SSE: TryWrite to SSE channel
    TB->>CA: TryWrite to aggregator channel

    SSE-->>FE: event: marketdata (tick)
    FE->>FE: Update watchlist and ticker strip

    CA->>CA: Update in progress candle OHLCV
    alt 500ms throttle elapsed
        CA-->>SSE: TryWrite candle event
        SSE-->>FE: event: candlestick (in progress)
        FE->>FE: Update chart live candle
    end

    alt Minute boundary crossed
        CA->>CH: Push completed candle
        CA-->>SSE: TryWrite completed candle
        SSE-->>FE: event: candlestick (complete)
        FE->>FE: Finalise candle on chart
    end
```

---

## Dependency Graph — Service Startup Order

```mermaid
graph LR
    Redis[Redis Container] --> IngestionAPI[Ingestion API]
    Redis --> BrokerService[Broker Service]
    IngestionAPI --> MarketSimulator[Market Simulator]
    BrokerService --> WebFrontend[Web Frontend]

    style Redis fill:#e74c3c,color:#fff
    style IngestionAPI fill:#3498db,color:#fff
    style BrokerService fill:#9b59b6,color:#fff
    style MarketSimulator fill:#2ecc71,color:#fff
    style WebFrontend fill:#f39c12,color:#fff
```

Arrows represent "must be ready before" relationships enforced by Aspire's `WaitFor()` method.

---

## Channel Pipeline Diagram

```mermaid
flowchart LR
    subgraph Redis_Layer
        RS[(Redis Stream<br/>marketdata:stream)]
    end

    subgraph Consumer_Layer
        RSC[RedisStreamConsumer]
    end

    subgraph Fan_Out
        TB[TickBroadcaster]
    end

    subgraph Channel_Layer
        SC[SSE Channel<br/>Cap 1000 DropOldest]
        AC[Aggregator Channel<br/>Cap 2000 DropOldest]
    end

    subgraph Processing
        CA[CandlestickAggregator]
        CC[Candle Channel<br/>Cap 500 DropOldest]
    end

    subgraph SSE_Endpoints
        SSE1[SseController]
        SSE2[SseControllerV2]
    end

    RS --> RSC
    RSC --> TB
    TB --> SC
    TB --> AC
    AC --> CA
    CA --> CC

    SC --> SSE1
    CC --> SSE1
    SC --> SSE2

    style SC fill:#3498db,color:#fff
    style AC fill:#2ecc71,color:#fff
    style CC fill:#9b59b6,color:#fff
```

This diagram shows the bounded channel pipeline inside the Broker Service. Each channel has a fixed capacity and uses the DropOldest policy when full, ensuring the system never blocks or runs out of memory.
