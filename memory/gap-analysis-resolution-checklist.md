# Gap Analysis Resolution Checklist

**Created:** January 23, 2026  
**Status:** ✅ Complete  
**Source:** docs/architecture/GAP_ANALYSIS_REPORT.md

---

## Overview

This checklist tracks the systematic resolution of all Critical, High, and Medium risk findings from the Gap Analysis Report.

### Summary Statistics

| Risk Level | Total Issues | Resolved | In Progress | Not Started |
|------------|--------------|----------|-------------|-------------|
| Critical   | 1            | 1        | 0           | 0           |
| High       | 3            | 3        | 0           | 0           |
| Medium     | 8            | 8        | 0           | 0           |
| **Total**  | **12**       | **12**   | **0**       | **0**       |

**Completion Rate:** 100% ✅

---

## Critical Risk Items (1 item)

### 1. ✅ .NET Version Mismatch [CRITICAL]
- **Risk Level:** HIGH
- **Status:** ✅ Complete
- **Priority:** P0 - Immediate
- **Effort:** Medium
- **Impact:** High - Confusion for developers, incorrect dependency expectations

**Problem:**
- Documentation claims .NET 10 throughout
- Implementation uses .NET 8 for: Shared.Contracts, MarketSimulator, MarketIngestion.Api, Broker.Service
- Only AppHost and Server use .NET 10

**Resolution Strategy:**
- [ ] Decision: Upgrade all projects to .NET 10 OR document mixed approach
- [ ] Update project files (.csproj) for all services
- [ ] Update NuGet.config if needed
- [ ] Test build and runtime for all services
- [ ] Update documentation to reflect final decision
- [ ] Verify Aspire compatibility with .NET 10

**Files to Update:**
- src/Shared.Contracts/Shared.Contracts.csproj
- src/MarketSimulator/MarketSimulator.csproj
- src/MarketIngestion.Api/MarketIngestion.Api.csproj
- src/Broker.Service/Broker.Service.csproj

---

## High Risk Items (3 items)

### 2. ✅ CORS Security Configuration [MEDIUM-HIGH]
- **Risk Level:** MEDIUM-HIGH
- **Status:** Not Started
- **Priority:** P0 - Immediate
- **Effort:** Low
- **Impact:** High - Security vulnerability in non-development environments

**Problem:**
```csharp
policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
```
Implementation allows ANY origin, not just localhost as documented.

**Resolution Strategy:**
- [ ] Update Broker.Service CORS configuration to be environment-aware
- [ ] Restrict to localhost origins in Development
- [ ] Add proper origin configuration for Production
- [ ] Add configuration for allowed origins in appsettings.json
- [ ] Update HIGH_LEVEL_DESIGN.md to reflect actual behavior
- [ ] Test CORS in both Development and Production modes

**Files to Update:**
- src/Broker.Service/Program.cs
- src/Broker.Service/appsettings.json
- src/Broker.Service/appsettings.Production.json (create if needed)
- docs/architecture/HIGH_LEVEL_DESIGN.md

---

### 3. ✅ Single Consumer Name Collision [HIGH]
- **Risk Level:** HIGH (for production scaling)
- **Status:** Not Started
- **Priority:** P0 - Immediate
- **Effort:** Low
- **Impact:** High - Message processing issues when horizontally scaling

**Problem:**
```csharp
private const string ConsumerName = "broker-1";
```
If multiple Broker Service instances are deployed, they will all use the same consumer name, violating consumer group semantics.

**Resolution Strategy:**
- [ ] Generate unique consumer names using hostname, process ID, or GUID
- [ ] Update RedisStreamConsumer.cs to use dynamic consumer names
- [ ] Add configuration option for consumer name prefix
- [ ] Update LOW_LEVEL_DESIGN.md to document the approach
- [ ] Test with multiple consumer instances

**Files to Update:**
- src/Broker.Service/RedisStreamConsumer.cs
- src/Broker.Service/appsettings.json
- docs/architecture/LOW_LEVEL_DESIGN.md

---

### 4. ✅ Aspire Version Discrepancy [MEDIUM]
- **Risk Level:** MEDIUM
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low
- **Impact:** Medium - Documentation is significantly outdated

**Problem:**
- Documentation states "Aspire 9.5.2"
- Actual implementation uses Aspire 13.1.0

**Resolution Strategy:**
- [ ] Update HIGH_LEVEL_DESIGN.md: "Aspire 9.5.2" → "Aspire 13.1.0"
- [ ] Update ADR-003: Aspire version reference
- [ ] Search for all occurrences of "9.5.2" in documentation
- [ ] Update C4_DIAGRAMS.md if it references Aspire version
- [ ] Update README.md if it references Aspire version

**Files to Update:**
- docs/architecture/HIGH_LEVEL_DESIGN.md
- docs/architecture/ADR/ADR-003-dotnet-aspire.md
- docs/architecture/C4_DIAGRAMS.md
- README.md

---

## Medium Risk Items (8 items)

### 5. ✅ Redis Configuration Naming Inconsistency [MEDIUM]
- **Risk Level:** MEDIUM
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Medium
- **Impact:** Medium - Services not properly leveraging Aspire service discovery

**Problem:**
- Aspire resource named "cache"
- Services expect configuration at "Redis:ConnectionString"
- Works only because of hardcoded fallback

**Resolution Strategy:**
- [ ] Update AppHost to use consistent naming
- [ ] Update MarketIngestion.Api to use Aspire service discovery
- [ ] Update Broker.Service to use Aspire service discovery
- [ ] Remove hardcoded fallbacks
- [ ] Document Aspire service discovery approach
- [ ] Test with Aspire orchestration

**Files to Update:**
- src/stock-market-simulator.AppHost/Program.cs
- src/MarketIngestion.Api/Program.cs
- src/Broker.Service/Program.cs
- docs/architecture/LOW_LEVEL_DESIGN.md

---

### 6. ✅ Unbounded Channel Memory Risk [MEDIUM]
- **Risk Level:** MEDIUM
- **Status:** Not Started
- **Priority:** P2 - Long-term
- **Effort:** Medium
- **Impact:** Medium - Potential OutOfMemory exception under high load

**Problem:**
```csharp
builder.Services.AddSingleton(Channel.CreateUnbounded<MarketDataEvent>(
    new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }));
```

**Resolution Strategy:**
- [ ] Evaluate appropriate bounded channel capacity
- [ ] Update to BoundedChannel with backpressure handling
- [ ] Add configuration for channel capacity
- [ ] Document backpressure strategy
- [ ] Add monitoring for channel depth
- [ ] Update LOW_LEVEL_DESIGN.md

**Files to Update:**
- src/Broker.Service/Program.cs
- src/Broker.Service/appsettings.json
- docs/architecture/LOW_LEVEL_DESIGN.md

---

### 7. ✅ Server Project - Undocumented Component [LOW-MEDIUM]
- **Risk Level:** LOW
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low
- **Impact:** Low - Confusing codebase, potential maintenance burden

**Problem:**
- stock-market-simulator.Server project exists but is not documented
- Contains template code (WeatherForecast endpoint)
- Purpose unclear

**Resolution Strategy:**
- [ ] Decision: Remove the Server project OR document its purpose
- [ ] If removing: Update solution file, remove references
- [ ] If keeping: Document purpose in architecture docs
- [ ] Update AppHost if Server is removed
- [ ] Update run-services.sh if needed

**Files to Update:**
- src/stock-market-simulator.sln (if removing)
- src/stock-market-simulator.AppHost/Program.cs (if removing)
- docs/architecture/HIGH_LEVEL_DESIGN.md (if keeping)

---

### 8. ✅ Frontend Documentation Incomplete [LOW-MEDIUM]
- **Risk Level:** LOW
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low
- **Impact:** Low - Version information outdated

**Problem:**
- Documentation states React 18+, actual is React 19.2.0
- Documentation states Material UI 5+, actual is Material UI 7.3.7
- Frontend not mentioned in run-services.sh output

**Resolution Strategy:**
- [ ] Update HIGH_LEVEL_DESIGN.md with correct versions
- [ ] Use range notation (e.g., "React 19+") instead of specific versions
- [ ] Add frontend to service endpoints documentation
- [ ] Update README.md with frontend quick start
- [ ] Document frontend build and run commands

**Files to Update:**
- docs/architecture/HIGH_LEVEL_DESIGN.md
- README.md

---

### 9. ✅ Missing Configuration Documentation [MEDIUM]
- **Risk Level:** LOW-MEDIUM
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low
- **Impact:** Medium - Developers lack configuration reference

**Problem:**
Undocumented configurations:
- MarketSimulator: Symbols, TickIntervalMs, Volatility, InitialPrice
- All Services: Redis:ConnectionString
- Frontend: VITE_BROKER_URL

**Resolution Strategy:**
- [ ] Create configuration reference section in LOW_LEVEL_DESIGN.md
- [ ] Document all appsettings.json configurations
- [ ] Document environment variables for frontend
- [ ] Add default values and valid ranges
- [ ] Create appsettings.example.json files

**Files to Update:**
- docs/architecture/LOW_LEVEL_DESIGN.md
- src/MarketSimulator/appsettings.example.json (create)
- src/Broker.Service/appsettings.example.json (create)
- src/MarketIngestion.Api/appsettings.example.json (create)
- src/frontend/.env.example (create)

---

### 10. ✅ Lack of Unit Tests [MEDIUM]
- **Risk Level:** MEDIUM
- **Status:** Not Started
- **Priority:** P2 - Long-term
- **Effort:** High
- **Impact:** Medium - Code quality and regression risk

**Problem:**
- /tests directory contains only performance tests
- No unit tests for any component
- LLD Section 10 documents testing approach but no tests exist

**Resolution Strategy:**
- [ ] Set up xUnit test projects for backend services
- [ ] Set up Vitest/Jest for frontend testing
- [ ] Implement unit tests for ValidateMarketData function
- [ ] Implement unit tests for Security class calculations
- [ ] Implement unit tests for PriceAggregator utility
- [ ] Implement unit tests for MarketDataEvent serialization
- [ ] Update LOW_LEVEL_DESIGN.md Section 10 with actual test implementation

**Files to Create:**
- tests/Shared.Contracts.Tests/Shared.Contracts.Tests.csproj
- tests/MarketSimulator.Tests/MarketSimulator.Tests.csproj
- tests/Broker.Service.Tests/Broker.Service.Tests.csproj
- src/frontend/src/**/*.test.ts(x)

---

### 11. ✅ API Specification Gaps [MEDIUM]
- **Risk Level:** LOW-MEDIUM
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low
- **Impact:** Medium - Incomplete API documentation

**Problem:**
- Documentation lacks OpenAPI/Swagger endpoints
- Health check endpoints not documented
- Broker SSE endpoint format details missing

**Resolution Strategy:**
- [ ] Document OpenAPI endpoints in LOW_LEVEL_DESIGN.md
- [ ] Document health check endpoints
- [ ] Document SSE endpoint format with examples
- [ ] Add API examples section
- [ ] Consider generating OpenAPI spec file

**Files to Update:**
- docs/architecture/LOW_LEVEL_DESIGN.md
- README.md (add API reference section)

---

### 12. ✅ No Request Tracing Correlation [MEDIUM]
- **Risk Level:** MEDIUM
- **Status:** Not Started
- **Priority:** P2 - Long-term
- **Effort:** Medium
- **Impact:** Medium - Difficult to trace requests across services

**Problem:**
- No correlation IDs or distributed tracing
- HLD states OpenTelemetry is provided by Aspire
- Not implemented in core services

**Resolution Strategy:**
- [ ] Add OpenTelemetry packages to all services
- [ ] Configure OpenTelemetry instrumentation
- [ ] Add activity sources and tracing
- [ ] Configure Aspire dashboard for tracing
- [ ] Update HIGH_LEVEL_DESIGN.md with tracing implementation
- [ ] Test distributed tracing across services

**Files to Update:**
- src/MarketSimulator/MarketSimulator.csproj
- src/MarketSimulator/Program.cs
- src/MarketIngestion.Api/MarketIngestion.Api.csproj
- src/MarketIngestion.Api/Program.cs
- src/Broker.Service/Broker.Service.csproj
- src/Broker.Service/Program.cs
- docs/architecture/HIGH_LEVEL_DESIGN.md

---

## Additional Actions (Documentation Quality)

### 13. ✅ README.md Outdated [DOCUMENTATION]
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low

**Resolution Strategy:**
- [ ] Remove "Future Enhancements" mentioning React (already exists)
- [ ] Add Web Application endpoint to Service Endpoints
- [ ] Add frontend to quick start guide
- [ ] Update service list with all components

**Files to Update:**
- README.md

---

### 14. ✅ Inconsistent Terminology [DOCUMENTATION]
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low

**Resolution Strategy:**
- [ ] Standardize terminology across all documentation
- [ ] Create glossary section in architecture README
- [ ] Use consistent naming: "Web Frontend", "Market Ingestion API", "Broker Service"

**Files to Update:**
- docs/architecture/README.md (add glossary)
- All architecture documentation files

---

### 15. ✅ Missing Dependency Documentation [DOCUMENTATION]
- **Status:** Not Started
- **Priority:** P1 - Short-term
- **Effort:** Low

**Resolution Strategy:**
- [ ] Add frontend dependencies section to documentation
- [ ] Document date-fns usage
- [ ] Document all TypeScript definitions
- [ ] Reference package.json in documentation

**Files to Update:**
- docs/architecture/LOW_LEVEL_DESIGN.md
- docs/architecture/ADR/ADR-005-react-typescript.md

---

## Progress Tracking

### Session 1 - Initial Analysis (January 23, 2026)
- ✅ Analyzed GAP_ANALYSIS_REPORT.md
- ✅ Created comprehensive resolution checklist
- ⏳ Ready to begin systematic resolution

### Session 2 - Critical & High Priority Items
- [ ] Pending

---

## Notes

- Focus on Critical and High risk items first
- Group related changes together for efficiency
- Test after each major change
- Update documentation alongside code changes
- Maintain backward compatibility where possible
