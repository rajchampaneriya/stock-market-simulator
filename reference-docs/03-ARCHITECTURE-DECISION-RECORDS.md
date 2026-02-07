# Architecture Decision Records (ADR)

> **Purpose:** This document captures the key architectural decisions made during the design and implementation of the Stock Market Simulator project. Each ADR follows a lightweight format: context, decision, consequences.

---

## ADR-001: Redis Streams as the Message Broker

**Status:** Accepted  
**Date:** 2025-01

### Context

The system needs a message broker to decouple the data producer (Market Simulator) from the data consumer (Broker Service). Options considered:

| Option | Pros | Cons |
|---|---|---|
| Apache Kafka | Industry standard, durable, partitioned | Heavy operational overhead, complex for a single-host demo |
| RabbitMQ | Mature, rich routing | Not a stream/log (messages are deleted after consumption) |
| Redis Streams | Built-in consumer groups, lightweight, already available for caching, append-only log semantics | Less durable than Kafka, single-node for local dev |
| In-process channels only | Simplest | Cannot scale beyond one process, no replay |

### Decision

Use **Redis Streams** as the message broker.

### Consequences

- **Positive:** Zero additional infrastructure beyond a Redis container. Consumer groups (`XREADGROUP`) enable horizontal scaling of broker instances. Append-only log allows message replay. Stream trimming (`MAXLEN ~10000`) bounds memory.
- **Negative:** For true production workloads with strict durability, Redis persistence (AOF/RDB) would need to be configured. Not suitable for multi-datacenter replication without Redis Cluster or Sentinel.
- **Trade-off:** Acceptable for a simulation/demonstration system. Upgrading to Kafka later would require changing only the producer and consumer code, not the downstream SSE or frontend layers.

---

## ADR-002: Server-Sent Events (SSE) over WebSockets

**Status:** Accepted  
**Date:** 2025-01

### Context

Real-time market data must be pushed from the server to the browser. Two main approaches were evaluated:

| Criteria | SSE | WebSocket |
|---|---|---|
| Direction | Server → Client (unidirectional) | Bidirectional |
| Protocol | HTTP/1.1 standard | Upgraded HTTP connection |
| Auto-reconnect | Built into the EventSource API | Must be implemented manually |
| Last-Event-ID replay | Built into the specification | Must be implemented manually |
| Browser support | All modern browsers | All modern browsers |
| Complexity | Lower | Higher |
| Load balancer compatibility | Standard HTTP | Requires sticky sessions or upgrade support |

### Decision

Use **Server-Sent Events (SSE)** for streaming market data from the Broker Service to the frontend.

### Consequences

- **Positive:** Simpler implementation. Browser `EventSource` API handles reconnection automatically. `Last-Event-ID` header enables missed event replay. Works through standard HTTP proxies and load balancers.
- **Negative:** Unidirectional only — if bidirectional communication is needed later (e.g., user placing trades), WebSockets or a separate REST endpoint would be required.
- **Trade-off:** The current system is read-only (view market data), making SSE the ideal fit.

---

## ADR-003: .NET Aspire for Service Orchestration

**Status:** Accepted  
**Date:** 2025-01

### Context

The system comprises multiple services (Simulator, Ingestion API, Broker, Redis, Frontend) that must start in a specific order with correct configuration injected. Options:

| Option | Pros | Cons |
|---|---|---|
| Docker Compose | Well-known, language-agnostic | Requires containerizing all services; no service discovery without extra tooling |
| Manual startup scripts | Simple | Error-prone, no health checks, no dependency ordering |
| .NET Aspire | First-class .NET integration, automatic service discovery, health check support, resource lifecycle management | .NET-specific, newer ecosystem |

### Decision

Use **.NET Aspire 13.1** as the application host and orchestrator.

### Consequences

- **Positive:** Declarative resource graph in C# (`builder.AddProject`, `builder.AddRedis`). Automatic dependency resolution via `WaitFor()`. Connection strings injected automatically via `WithReference()`. Redis is provisioned with a data volume and Commander UI. Vite app is managed via `AddViteApp()`.
- **Negative:** Requires .NET-specific tooling. Cannot orchestrate non-.NET services without special adapters (though JavaScript/Vite is supported).
- **Trade-off:** Excellent developer experience for a .NET-centric system.

---

## ADR-004: Geometric Brownian Motion with Mean Reversion for Price Simulation

**Status:** Accepted  
**Date:** 2025-01

### Context

The simulator needs to produce price data that appears realistic when displayed on candlestick charts. Simple random walks produce unrealistic drift; constant prices are not useful for visual demonstration.

### Decision

Combine three financial models:

1. **Geometric Brownian Motion (GBM):** Multiplicative random walk producing log-normal price distributions.
2. **Ornstein-Uhlenbeck Mean Reversion:** Pulls prices back toward a base value, preventing runaway drift.
3. **GARCH Volatility Clustering:** Makes volatile periods cluster together (high-volatility ticks tend to follow other high-volatility ticks).

Additionally, overlay **micro-trends** (random directional drift lasting 12–60 seconds) to create recognizable chart patterns.

### Consequences

- **Positive:** Charts look professionally realistic. Prices stay within reasonable bounds. Volatility behaviour matches real markets. Per-symbol profiles allow different securities to behave differently.
- **Negative:** More complex than a simple random walk. Requires understanding of the financial mathematics for maintenance.
- **Trade-off:** The complexity is isolated to one class (`RealisticPriceEngine`) and is well-commented.

---

## ADR-005: Bounded Channels with DropOldest for Backpressure

**Status:** Accepted  
**Date:** 2025-01

### Context

The Broker Service has multiple internal pipelines: Redis → Broadcaster → SSE Channel + Aggregator Channel → Controllers. If a slow consumer (e.g., a client on a poor connection) cannot keep up, the system must not block the producer or exhaust memory.

### Decision

Use `System.Threading.Channels.Channel.CreateBounded<T>` with `BoundedChannelFullMode.DropOldest` for all internal channels:

| Channel | Capacity |
|---|---|
| SSE Tick Channel | 1000 |
| Aggregator Tick Channel | 2000 |
| Candle Channel | 500 |

### Consequences

- **Positive:** Memory is bounded. Producer is never blocked. System remains responsive under load.
- **Negative:** Stale ticks may be dropped if consumers fall behind. This is acceptable for a real-time display where only the latest prices matter.
- **Trade-off:** Losing an old tick is preferable to crashing or freezing.

---

## ADR-006: Backend Candlestick Aggregation

**Status:** Accepted  
**Date:** 2025-02

### Context

Initially, OHLCV candles were aggregated in the frontend from raw ticks. This had problems:
- Duplicate ticks sent to multiple SSE channels caused candle inconsistency
- Frontend could not reliably determine candle boundaries
- CPU-intensive aggregation in the browser

### Decision

Move candlestick aggregation to a **backend `CandlestickAggregator` service** that:
1. Receives ticks via a dedicated channel
2. Builds 1-minute OHLCV candles server-side
3. Streams candle events via a separate SSE event type (`candlestick`)
4. Throttles in-progress updates to 500 ms intervals

### Consequences

- **Positive:** Single source of truth for candles. Consistent across all connected clients. Reduces frontend complexity. Enables pre-seeding historical candles on connection.
- **Negative:** Additional backend complexity (CandlestickAggregator, CandlestickHistory, AggregatorTickChannel, TickBroadcaster).
- **Trade-off:** Worth it for data consistency and better user experience.

---

## ADR-007: React with D3.js for Charting

**Status:** Accepted  
**Date:** 2025-01

### Context

The frontend needs professional-quality candlestick charts. Options:

| Option | Pros | Cons |
|---|---|---|
| TradingView Lightweight Charts | Production quality, well-maintained | GPL licence, limited customisation |
| Recharts | Popular, React-native | No candlestick support out of box |
| D3.js (custom) | Full control, no licence restrictions | Higher implementation effort |
| Chart.js | Simple, plugin ecosystem | Limited financial chart support |

### Decision

Use **D3.js with custom rendering** for candlestick charts.

### Consequences

- **Positive:** Complete control over visual appearance (TradingView-inspired dark theme). Custom crosshair and tooltip behaviour. No licence concerns. Volume bars, price lines, and responsive layout all customised.
- **Negative:** More code to write and maintain than a pre-built library.
- **Trade-off:** Acceptable for a project that showcases technical capability.

---

## ADR-008: Immutable Records for Event Data Models

**Status:** Accepted  
**Date:** 2025-01

### Context

Data models like `MarketDataEvent` and `CandlestickEvent` are serialised, transmitted over HTTP and SSE, and consumed by multiple threads.

### Decision

Use C# `record` types with `init`-only properties for all event data models.

### Consequences

- **Positive:** Thread-safe by design (immutability). Built-in value equality. Cleaner JSON serialisation semantics. Self-documenting intent.
- **Negative:** Cannot be modified in place (must create new instances for changes).
- **Trade-off:** Mutable `Security` class is used only for the local in-memory cache where updates are frequent and thread-safety is managed by the single-writer pattern.

---

## ADR-009: Historical Candle Seeding on SSE Connection

**Status:** Accepted  
**Date:** 2025-02

### Context

When a new client connects via SSE, the candlestick chart would be empty until enough ticks arrive to form candles (about 1 minute for the first candle). This creates a poor first impression.

### Decision

Generate **30 synthetic historical candles** per symbol using the same GBM model as the Market Simulator, and replay them to each new SSE client immediately after connection.

### Consequences

- **Positive:** Charts have visual density immediately. Users see a realistic-looking chart from the first second. Historical data is deterministic per symbol (seeded by symbol hash).
- **Negative:** The historical data is synthetic, not based on actual past simulation runs.
- **Trade-off:** For a demo/simulation, synthetic history that looks realistic is more valuable than an empty chart.

---

## ADR-010: OpenTelemetry for Distributed Tracing

**Status:** Accepted  
**Date:** 2025-01

### Context

With multiple services communicating over HTTP and Redis, debugging issues requires visibility into cross-service request flows.

### Decision

Integrate **OpenTelemetry** with OTLP exporters in all three .NET services:
- `MarketSimulator`: HTTP client instrumentation
- `MarketIngestion.Api`: ASP.NET Core + HTTP + Redis instrumentation
- `Broker.Service`: ASP.NET Core + HTTP + Redis instrumentation

### Consequences

- **Positive:** End-to-end request tracing. Exception recording. Console export for local debugging. OTLP export for integration with Jaeger, Grafana Tempo, or Azure Monitor.
- **Negative:** Adds dependencies and slight performance overhead.
- **Trade-off:** Essential for any distributed system. Overhead is negligible for this throughput level.
