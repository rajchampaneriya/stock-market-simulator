# 📘 Product Requirements Document

## Real-Time Market Data Streaming System

**Author:** Raj Champaneriya
**Platform:** .NET 10 + Aspire
**Purpose:** Educational / Architecture Demonstration
**Audience:** Engineers, Architects, Hiring Managers, Trading-Tech Enthusiasts

---

## 1. 🎯 Objective

Build a **minimal but realistic prototype** that demonstrates:

* How real-time stock market data flows
* How streaming systems are designed
* How brokers distribute live prices
* How modern .NET + Aspire can model real-world event-driven systems

This is **not a trading system**.
This is an **architecture + streaming demonstration**.

This prototype demonstrates a complete stock market data ecosystem, simulating the flow of market data from exchanges through different types of brokers to end users. The implementation showcases various architectural patterns and technologies commonly used in real-world financial systems, including:

- Event-driven architecture for real-time data processing
- Publisher-subscriber pattern for data distribution
- Different broker types with varying requirements
- Web-based client using Server-Sent Events for real-time updates

The prototype can be extended with additional features such as:

- Historical data storage and retrieval
- More sophisticated trading algorithms
- Authentication and authorization
- Performance monitoring and metrics
- Integration with real market data feeds

---

## 2. 🧠 Problem Statement

Most engineers understand CRUD systems, but very few understand:

* Event-driven streaming
* Fan-out architectures
* Near real-time data delivery
* Market data ingestion
* Low-latency system design

This project aims to visually and technically demonstrate:

> “How price data moves from exchange → broker → trader”

This prototype demonstrates a complete stock market data ecosystem using .NET 10 and .NET Aspire. The system simulates the flow of market data from exchanges through brokers to end users, showcasing the architecture and technologies discussed in the blog post.

---

## 3. 🧩 High-Level Architecture

```
┌─────────────┐
│ Market Feed │  (Simulated Exchange - .NET Console)
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│ Ingestion API   │  (.NET 10 Minimal API)
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ Event Stream    │  (Redis Streams)
└──────┬──────────┘
       │
 ┌─────┴───────────────────────┐
 │                             │
 ▼                             ▼
Broker API               Analytics Engine
(SSE-Server Side Events)    (Aggregation)
 │
 ▼
Web UI (Live Prices)
```

---

## 4. 🏗️ System Components

### 4.1 Market Data Generator (Exchange Simulator)

**Purpose:**
Simulate stock exchange ticks.

**Tech:**

* .NET 10 Worker Service
* BackgroundService
* Randomized tick generation

**Responsibilities:**

* Generate price ticks
* Emit events every 200–500ms
* Publish to Event Stream

**Sample Event**

```json
{
  "symbol": "NIFTY50",
  "price": 22435.50,
  "volume": 1200,
  "timestamp": "2026-01-21T10:15:23.232Z"
}
```

---

### 4.2 Market Ingestion Service

**Purpose:**
Acts like NSE / Exchange feed handler.

**Responsibilities:**

* Accepts raw tick data
* Validates format
* Publishes to stream (Redis)

**Tech Stack**

* .NET 10 Minimal API
* Aspire Service Discovery
* OpenTelemetry enabled

---

### 4.3 Streaming Layer

**Options (Choose One):**

| Option           | Use Case           |
| ---------------- | ------------------ |
| Redis Streams    | Simple, fast demo  |
| Kafka            | Real-world realism |
| Azure Event Hubs | Cloud-ready        |

✅ **Recommended for demo:** Redis Streams

---

### 4.4 Broker Service

**Purpose:**
Acts like Zerodha / IBKR backend.

**Responsibilities:**

* Subscribe to stream
* Cache latest prices
* Serve WebSocket clients
* Apply throttling logic

**Key Features**

* Symbol-based subscription
* Rate-limited broadcasting
* Snapshot + delta updates

**Tech**

* .NET Core
* ReactJS
* Server Side Events
* Redis Cache

---

### 4.5 Web UI (Trader View)

**Purpose:**
Visualize real-time streaming data.

**Features**

* Live price updates
* Price change animation
* Connection status
* Latency indicator

**Tech**

* ReactJS
* Server Side Events
* Material UI CSS
* Candle Sick Chart with volume - d3js 

---

## 5. 🔄 Data Flow (End-to-End)

```
[Exchange Simulator]
        │
        ▼
[Ingestion API]
        │
        ▼
[Redis Stream]
        │
        ▼
[Broker Service]
        │
        ▼
[Server Side Events]
        │
        ▼
[Web UI]
```

---

## 6. 🧠 Architecture Principles Used

✅ Event-driven
✅ Eventually consistent
✅ Push-based (not polling)
✅ Horizontally scalable
✅ Fault-tolerant
✅ Observable

---

## 7. 📊 Non-Functional Requirements

| Category      | Requirement          |
| ------------- | -------------------- |
| Latency       | < 300ms              |
| Scalability   | 10k+ simulated users |
| Reliability   | Auto-reconnect       |
| Observability | OpenTelemetry        |
| Deployability | Docker + Aspire      |
| Cost          | Local-first          |

---

## 8. 📦 Aspire Project Structure

```
/src
 ├── MarketSimulator
 ├── MarketIngestion.Api
 ├── Broker.Service
 ├── WebApp
 ├── Shared.Contracts
 └── AppHost (Aspire)
```

---

## 9. 📡 Telemetry & Observability

Enabled via Aspire:

* OpenTelemetry
* Metrics
* Traces
* Request latency
* Message throughput
* Connection count

---

## 10. 🧪 Phase-wise Implementation Plan

### Phase 1 – Core Flow

* [ ] Exchange simulator
* [ ] Ingestion API
* [ ] Redis stream
* [ ] WebSocket push

### Phase 2 – Realism

* [ ] Multiple symbols
* [ ] Order book simulation
* [ ] Throttling
* [ ] Disconnect handling

### Phase 3 – Showcase

* [ ] UI charts
* [ ] Metrics dashboard
* [ ] Architecture diagram
* [ ] Blog walkthrough

---

## 11. 🎯 Why This Project Matters (For Your Brand)

This demonstrates:

✔ Real-time system understanding
✔ Streaming architecture knowledge
✔ .NET + Aspire expertise
✔ Finance + Tech crossover
✔ Production-grade thinking

This is **exactly** the kind of project that:

* Internal reviewers respect
* Architects relate to
* Hiring managers remember

---

## 12. 📌 Optional Enhancements (Later)

* FIX protocol simulation
* Market replay mode
* Latency heatmap
* WebSocket vs SSE comparison
* Cloud deployment (Azure Container Apps)
* Analytics and data pipelines using Spark based open source platform and Apache Iceberg suitable to demo unstructured data like quarterly financial reporting data and add module to show financial fundamental data viewer. 