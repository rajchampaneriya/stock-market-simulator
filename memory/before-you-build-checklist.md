# Before-You-Build Checklist
## Stock Market Data Streaming System

**Purpose:** Comprehensive pre-implementation validation checklist  
**Date:** January 21, 2026  
**Audience:** Development team preparing to implement the system  
**Status:** Ready for use

---

## ✅ How to Use This Checklist

1. **Read through entire checklist before starting implementation**
2. **Complete sections in order** - dependencies exist between sections
3. **Check off items as completed** - track your progress
4. **Flag blockers immediately** - escalate if unable to complete critical items
5. **Update this document** - add notes and learnings as you go

---

## 1. 📋 Documentation Review

### 1.1 Requirements Understanding
- [ ] **Read objective.md completely** - Understand the vision and goals
- [ ] **Read prd.md completely** - Understand all functional and non-functional requirements
- [ ] **Read draft-technical-approach.md** - Understand the technical implementation
- [ ] **Review architecture diagrams** - Understand data flow and component interactions
- [ ] **Identify all components to build:**
  - [ ] Market Data Generator (Exchange Simulator)
  - [ ] Market Ingestion Service
  - [ ] Redis Streams (configuration)
  - [ ] Broker Service
  - [ ] Web UI (React)
  - [ ] Aspire AppHost
- [ ] **Understand phase-wise plan** - Know what goes in MVP vs future enhancements

**Sign-off:** ___________ **Date:** ___________

---

## 2. 🎯 Scope and Constraints

### 2.1 MVP Scope Clarity
- [ ] **Confirmed: This is a minimal prototype** - Not a production trading system
- [ ] **Confirmed: Simple data model** - Symbol, Price, Volume, Timestamp only
- [ ] **Confirmed: MVP excludes:**
  - [ ] Bid/ask spreads (future enhancement)
  - [ ] Order book simulation (future enhancement)
  - [ ] Multiple broker types (future enhancement)
  - [ ] FIX protocol (future enhancement)
  - [ ] Historical data storage (future enhancement)
  - [ ] Authentication/authorization (future enhancement)
- [ ] **Confirmed: Focus on core flow** - Exchange → Ingestion → Redis → Broker → UI
- [ ] **Confirmed: Educational purpose** - Demonstrate architecture, not build production system

**Notes:**
```
_______________________________________________________________________
_______________________________________________________________________
```

---

## 3. 🛠️ Development Environment Setup

### 3.1 Software Prerequisites
- [ ] **.NET 10 SDK installed** - Verify: `dotnet --version` shows 10.x
- [ ] **.NET Aspire workload installed** - Run: `dotnet workload install aspire`
- [ ] **Docker Desktop installed and running** - Required for Redis container
- [ ] **Node.js 20+ installed** - For React frontend, verify: `node --version`
- [ ] **npm or yarn installed** - Verify: `npm --version` or `yarn --version`
- [ ] **Git installed** - For version control
- [ ] **IDE/Editor ready:**
  - [ ] Visual Studio 2022 (17.10+) OR
  - [ ] Visual Studio Code with C# Dev Kit OR
  - [ ] JetBrains Rider 2024+
- [ ] **Redis CLI installed** (optional but helpful) - For debugging

**Environment validation commands:**
```bash
dotnet --version                  # Should show 10.x
dotnet workload list              # Should show 'aspire' installed
docker --version                  # Should show Docker version
node --version                    # Should show 20+
npm --version                     # Should show latest
```

### 3.2 Repository Setup
- [ ] **Clone repository locally**
- [ ] **Create feature branch** - From main/master branch
- [ ] **Verify .gitignore is comprehensive:**
  - [ ] bin/ and obj/ directories
  - [ ] node_modules/
  - [ ] .vs/ and .vscode/ (IDE folders)
  - [ ] *.user files
  - [ ] Temporary files

**Commands:**
```bash
git clone <repository-url>
cd stock-market-simulator
git checkout -b feature/implementation
```

---

## 4. 🏗️ Architecture Understanding

### 4.1 Component Dependencies
- [ ] **Understand data flow:**
  ```
  Exchange Simulator → (HTTP POST) → Ingestion API → (Redis XADD) → 
  Redis Streams → (XREADGROUP) → Broker Service → (SSE) → Web UI
  ```
- [ ] **Understand technology choices:**
  - [ ] Why Redis Streams? (Simple, fast, demo-appropriate)
  - [ ] Why SSE vs WebSockets? (One-way push, simpler, auto-reconnect)
  - [ ] Why .NET Aspire? (Orchestration, service discovery, observability)
  - [ ] Why React? (Popular, good for real-time UIs)

### 4.2 Key Architectural Decisions
- [ ] **Decision 1: Simple data model**
  - Rationale: MVP focus, realistic but minimal
  - Impact: Easy to understand and implement
- [ ] **Decision 2: Redis Streams over Kafka**
  - Rationale: Local-first, simpler setup
  - Impact: Sufficient for 10k+ simulated users
- [ ] **Decision 3: SSE over WebSockets**
  - Rationale: One-way push, automatic reconnection
  - Impact: Simpler client implementation
- [ ] **Decision 4: Aspire orchestration**
  - Rationale: Modern .NET approach, built-in observability
  - Impact: Easier service management and monitoring

**Validation Questions (answer before proceeding):**
1. Why do we need the Ingestion API instead of simulator directly writing to Redis?
   ```
   Answer: _______________________________________________________________
   ```
2. What happens if a broker crashes? Will we lose data?
   ```
   Answer: _______________________________________________________________
   ```
3. How do multiple web clients receive the same data?
   ```
   Answer: _______________________________________________________________
   ```

---

## 5. 📊 Non-Functional Requirements Validation

### 5.1 Performance Targets
- [ ] **Latency target: < 300ms end-to-end (p95)**
  - Understand how to measure: Use OpenTelemetry traces
  - Know where latency can occur: Network, Redis, SSE transmission
- [ ] **Throughput target: 10+ events/sec per symbol**
  - Understand testing approach: Load testing with multiple symbols
- [ ] **Scalability target: 10,000+ simulated concurrent users**
  - Understand how to test: Load testing tools (k6, JMeter, or custom)

### 5.2 Reliability Requirements
- [ ] **Auto-reconnection required:**
  - Web UI must reconnect to broker on disconnect
  - Broker must reconnect to Redis on failure
- [ ] **Graceful failure handling:**
  - Services should log errors and continue
  - No cascading failures
- [ ] **Data integrity:**
  - Redis Streams provides ordering
  - Consumer groups ensure message processing

### 5.3 Observability Requirements
- [ ] **OpenTelemetry traces enabled** - Aspire provides this
- [ ] **Metrics to track:**
  - Message throughput (events/sec)
  - Connection count (active clients)
  - Latency percentiles (p50, p95, p99)
- [ ] **Aspire dashboard accessible** - Verify after setup

---

## 6. 🔐 Security and Privacy

### 6.1 Security Considerations (Even for Prototype)
- [ ] **No hardcoded secrets** - Use configuration, environment variables, or Aspire secrets
- [ ] **Redis connection string** - Store in appsettings.json or environment variable
- [ ] **API endpoints** - No sensitive data exposure
- [ ] **Input validation** - Ingestion API must validate all inputs
- [ ] **CORS configuration** - Properly configure for Web UI

### 6.2 Data Privacy
- [ ] **No real market data** - Using simulated data only
- [ ] **No PII** - System doesn't collect personal information
- [ ] **No authentication required** - MVP is open access (can add later)

**Security Checklist:**
- [ ] Reviewed for SQL injection vectors (none - no SQL database)
- [ ] Reviewed for XSS vectors (validate React rendering)
- [ ] Reviewed for excessive resource consumption (rate limiting in broker)
- [ ] No secrets in source control

---

## 7. 🗂️ Project Structure Planning

### 7.1 Solution Structure
```
StockMarketSimulator.sln
├── src/
│   ├── AppHost/                    (Aspire orchestration)
│   │   └── Program.cs
│   ├── Shared.Contracts/           (Shared data models)
│   │   ├── MarketDataEvent.cs
│   │   └── Security.cs
│   ├── MarketSimulator/            (Exchange simulator)
│   │   ├── ExchangeSimulator.cs
│   │   └── Program.cs
│   ├── MarketIngestion.Api/        (Ingestion service)
│   │   └── Program.cs
│   ├── Broker.Service/             (Broker with SSE)
│   │   ├── RedisStreamConsumer.cs
│   │   ├── Controllers/
│   │   │   └── SseController.cs
│   │   └── Program.cs
│   └── WebApp/                     (React frontend)
│       ├── public/
│       ├── src/
│       │   ├── components/
│       │   ├── hooks/
│       │   └── App.tsx
│       └── package.json
├── tests/                          (Optional - can add later)
├── docs/
│   ├── requirements/
│   ├── plan/
│   └── post/
└── memory/                         (Working documents)
```

- [ ] **Verified structure** - Matches Aspire best practices
- [ ] **Shared.Contracts first** - Start with data contracts
- [ ] **Build order planned:**
  1. Shared.Contracts
  2. AppHost (with Redis)
  3. MarketSimulator
  4. MarketIngestion.Api
  5. Broker.Service
  6. WebApp

---

## 8. 📦 Dependencies and Packages

### 8.1 NuGet Packages Required

**Shared.Contracts:**
- No additional packages (just .NET 10 core)

**MarketSimulator:**
- [ ] `Microsoft.Extensions.Hosting`
- [ ] `Microsoft.Extensions.Http`

**MarketIngestion.Api:**
- [ ] `StackExchange.Redis` (for Redis Streams)
- [ ] `Aspire.StackExchange.Redis`

**Broker.Service:**
- [ ] `StackExchange.Redis`
- [ ] `Aspire.StackExchange.Redis`
- [ ] `Microsoft.AspNetCore.Mvc`

**AppHost:**
- [ ] `Aspire.Hosting.Redis`

### 8.2 NPM Packages Required

**WebApp:**
- [ ] `react` and `react-dom`
- [ ] `typescript`
- [ ] `@mui/material` (Material UI)
- [ ] `@emotion/react` and `@emotion/styled` (Material UI dependencies)
- [ ] `d3` and `@types/d3` (for charts)

### 8.3 Package Security
- [ ] **Run `dotnet list package --vulnerable`** - Check for vulnerabilities
- [ ] **Run `npm audit`** - Check for NPM vulnerabilities
- [ ] **Use latest stable versions** - Not pre-release versions

---

## 9. 🧪 Testing Strategy

### 9.1 Testing Approach
- [ ] **Unit tests:** Optional for MVP (focus on integration)
- [ ] **Integration tests:** Critical - Test component interactions
- [ ] **End-to-end tests:** Manual initially, automate later
- [ ] **Load tests:** Required to validate NFRs

### 9.2 Test Scenarios to Validate
- [ ] **Scenario 1: Basic flow**
  - Start all services
  - Verify data flows from simulator to UI
  - Check Aspire dashboard for traces
- [ ] **Scenario 2: Reconnection**
  - Stop broker service
  - Verify UI shows disconnected
  - Restart broker
  - Verify UI reconnects automatically
- [ ] **Scenario 3: Multiple symbols**
  - Configure 10+ symbols
  - Verify all update correctly in UI
- [ ] **Scenario 4: Performance**
  - Measure end-to-end latency
  - Verify < 300ms p95
  - Check resource usage

### 9.3 Testing Tools
- [ ] **Aspire Dashboard** - For observability and tracing
- [ ] **Redis CLI** - For debugging streams
- [ ] **Browser DevTools** - For SSE debugging
- [ ] **Postman/curl** - For API testing
- [ ] **k6 or JMeter** - For load testing (optional)

---

## 10. 📝 Configuration Management

### 10.1 Configuration Files Required

**MarketSimulator appsettings.json:**
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "IngestionApiUrl": "http://localhost:5001",
  "Symbols": ["NIFTY50", "BANKNIFTY", "RELIANCE", "TCS"],
  "TickIntervalMs": 300
}
```

**MarketIngestion.Api appsettings.json:**
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

**Broker.Service appsettings.json:**
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

- [ ] **Created all configuration files**
- [ ] **Configured for local development**
- [ ] **Added environment variable overrides**

### 10.2 Aspire Configuration
- [ ] **Redis port**: 6379 (default)
- [ ] **Service discovery enabled**
- [ ] **OpenTelemetry configured** (automatic with Aspire)

---

## 11. 🚀 Deployment Planning

### 11.1 Local Deployment (Primary)
- [ ] **Run via Aspire:** `dotnet run --project src/AppHost`
- [ ] **Verify Aspire dashboard:** http://localhost:15888 (or configured port)
- [ ] **Verify all services start:** Check Aspire dashboard
- [ ] **Access Web UI:** http://localhost:5000 (or configured port)

### 11.2 Docker Deployment (Optional)
- [ ] **Create Dockerfiles for each service**
- [ ] **Create docker-compose.yml**
- [ ] **Test: `docker compose up`**

### 11.3 Cloud Deployment (Future)
- [ ] Not required for MVP
- [ ] Can add Azure Container Apps support later

---

## 12. 📚 Knowledge Prerequisites

### 12.1 Technical Skills Required
- [ ] **.NET development:**
  - Minimal APIs
  - BackgroundService
  - HttpClient
  - Dependency injection
- [ ] **Redis knowledge:**
  - Redis Streams (XADD, XREADGROUP, XACK)
  - Consumer groups
  - Basic commands
- [ ] **React development:**
  - Hooks (useState, useEffect)
  - EventSource API for SSE
  - TypeScript basics
  - Material UI components
- [ ] **Aspire knowledge:**
  - Service orchestration
  - Service discovery
  - Dashboard usage

### 12.2 Learning Resources
- [ ] **Redis Streams:** https://redis.io/docs/data-types/streams/
- [ ] **Server-Sent Events:** https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events
- [ ] **.NET Aspire:** https://learn.microsoft.com/en-us/dotnet/aspire/
- [ ] **Material UI:** https://mui.com/
- [ ] **d3.js Candlestick:** https://d3-graph-gallery.com/graph/candle.html

**Time investment:**
- Redis Streams: 2-3 hours
- SSE: 1 hour
- Aspire: 2-3 hours
- Material UI + d3.js: 3-4 hours
- **Total: ~10 hours of learning**

---

## 13. ⚠️ Risk Assessment and Mitigation

### 13.1 Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Redis Streams unfamiliar | High | Medium | Study docs, use examples |
| Aspire is new framework | High | Medium | Follow tutorials, use templates |
| SSE implementation issues | Medium | Medium | Use tested patterns, test early |
| d3.js chart complexity | Medium | Medium | Start with simple examples |
| Performance not meeting NFRs | Low | High | Test early, optimize as needed |

### 13.2 Project Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Scope creep | High | High | **Strict MVP focus** |
| Over-engineering | Medium | High | Follow PRD exactly, resist abstractions |
| Time estimation wrong | Medium | Medium | Phase-wise approach, early testing |
| Component integration issues | Medium | High | Test integrations early |

### 13.3 Blockers Identified

Current blockers (must resolve before starting):
```
1. _________________________________________________________________
2. _________________________________________________________________
3. _________________________________________________________________
```

---

## 14. 📊 Success Criteria

### 14.1 MVP Definition of Done
- [ ] **All services running via Aspire**
- [ ] **Market data flows end-to-end:**
  - Simulator generates ticks
  - Ingestion API receives and validates
  - Redis Streams distributes events
  - Broker consumes and caches
  - Web UI displays real-time updates
- [ ] **Web UI shows:**
  - Live price updates for all configured symbols
  - Price change indicators (green/red)
  - Connection status
  - Basic candlestick chart with d3.js
- [ ] **Performance validated:**
  - End-to-end latency < 300ms (p95)
  - Handles 10+ events/sec per symbol
- [ ] **Observability working:**
  - Aspire dashboard shows all services
  - Traces visible for requests
  - No errors in logs (except expected)

### 14.2 Quality Gates
- [ ] **Code quality:**
  - Follows .NET naming conventions
  - Proper error handling
  - Logging at appropriate levels
  - No hardcoded values (use configuration)
- [ ] **Documentation:**
  - README.md with setup instructions
  - Code comments for complex logic
  - Architecture diagram updated if needed
- [ ] **Testing:**
  - Manual end-to-end test passed
  - Performance test passed
  - Reconnection scenario tested

---

## 15. 🎬 Go/No-Go Decision

### 15.1 Critical Prerequisites (Must be YES to proceed)
- [ ] ✅ **YES** / ❌ **NO**: Development environment fully setup
- [ ] ✅ **YES** / ❌ **NO**: All documentation read and understood
- [ ] ✅ **YES** / ❌ **NO**: Architecture and data flow clear
- [ ] ✅ **YES** / ❌ **NO**: MVP scope clearly defined and agreed
- [ ] ✅ **YES** / ❌ **NO**: Technical skills adequate or learning plan in place
- [ ] ✅ **YES** / ❌ **NO**: No critical blockers identified
- [ ] ✅ **YES** / ❌ **NO**: Testing strategy defined
- [ ] ✅ **YES** / ❌ **NO**: Success criteria clear

### 15.2 Go/No-Go Decision

**If ALL above are YES:**
```
✅ GO - Proceed with implementation
```

**If ANY above are NO:**
```
❌ NO-GO - Resolve blockers first
Blockers to resolve:
_______________________________________________________________________
_______________________________________________________________________
```

**Decision made by:** ___________ **Date:** ___________ **Time:** ___________

---

## 16. 📅 Implementation Timeline Estimate

### Phase 1: Foundation (2-3 days)
- Day 1: Setup solution, create projects, configure Aspire
- Day 2: Implement Shared.Contracts, basic simulator
- Day 3: Implement Ingestion API, test simulator → ingestion

### Phase 2: Distribution (2-3 days)
- Day 4: Implement Redis Streams publishing and consuming
- Day 5: Implement Broker Service with SSE
- Day 6: Test Redis → Broker → SSE flow

### Phase 3: UI (2-3 days)
- Day 7: Setup React app, basic SSE connection
- Day 8: Implement stock cards with Material UI
- Day 9: Add d3.js candlestick chart

### Phase 4: Testing & Polish (2-3 days)
- Day 10: Integration testing, bug fixes
- Day 11: Performance testing, optimization
- Day 12: Documentation, final review

**Total estimate: 8-12 days** (can be done in parallel by multiple developers)

---

## 17. 📞 Support and Escalation

### 17.1 Getting Help
- **Technical questions:** Check docs, Stack Overflow, GitHub issues
- **Blockers:** Escalate to team lead or architect
- **Design questions:** Refer back to objective.md and prd.md

### 17.2 Communication Plan
- **Daily standups:** Share progress and blockers
- **Demo after each phase:** Show working increments
- **Documentation updates:** Keep memory/ folder updated with learnings

---

## 18. ✅ Final Checklist Summary

**Before starting implementation, verify:**
- [ ] ✅ All sections of this checklist reviewed
- [ ] ✅ Development environment fully setup and tested
- [ ] ✅ All documentation read (objective.md, prd.md, draft-technical-approach.md)
- [ ] ✅ Architecture understood clearly
- [ ] ✅ MVP scope crystal clear
- [ ] ✅ No critical blockers
- [ ] ✅ Go/No-Go decision made: **GO**

---

**Checklist completed by:** ___________  
**Date:** ___________  
**Ready to begin implementation:** ✅ **YES** / ❌ **NO**

---

## 📋 Appendix: Quick Reference

### Useful Commands
```bash
# Verify .NET
dotnet --version
dotnet workload list

# Create Aspire solution
dotnet new aspire-starter -o StockMarketSimulator

# Run Aspire
dotnet run --project src/AppHost

# Redis CLI
redis-cli XRANGE marketdata:stream - +
redis-cli XINFO STREAM marketdata:stream

# Docker
docker ps
docker logs <container-id>

# Git
git status
git add .
git commit -m "message"
git push
```

### Key URLs (Local Development)
- Aspire Dashboard: http://localhost:15888
- Ingestion API: http://localhost:5001
- Broker Service: http://localhost:5002
- Web UI: http://localhost:5000
- Redis: localhost:6379

---

**End of Before-You-Build Checklist**
