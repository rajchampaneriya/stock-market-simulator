# Gap Analysis Report
## Stock Market Simulator - Architecture Review

**Version:** 1.0  
**Date:** January 2026  
**Prepared by:** Senior Architect Review  
**Status:** Complete

---

## Executive Summary

This document presents a comprehensive gap analysis between the architecture documentation in `/docs/architecture` and the actual implementation in `/src`. The review identifies inconsistencies, gaps, architectural risks, and deviations from best practices, along with actionable recommendations.

### Overall Assessment

| Category | Status | Severity |
|----------|--------|----------|
| Documentation Accuracy | ⚠️ Issues Found | Medium-High |
| Implementation Completeness | ✅ Complete | Low |
| Architectural Alignment | ⚠️ Issues Found | Medium |
| Best Practices Adherence | ⚠️ Issues Found | Medium |
| Security Posture | ⚠️ Issues Found | Medium-High |

---

## 1. Critical Gaps and Inconsistencies

### 1.1 .NET Version Mismatch (CRITICAL)

**Documentation Claims:**
- README.md, HIGH_LEVEL_DESIGN.md, C4_DIAGRAMS.md, and ADRs all reference **.NET 10**
- Documentation states ".NET 10.0.102+ SDK" as a prerequisite

**Actual Implementation:**
| Project | Documented Version | Actual Version |
|---------|-------------------|----------------|
| Shared.Contracts | .NET 10 | **.NET 8** (`net8.0`) |
| MarketSimulator | .NET 10 | **.NET 8** (`net8.0`) |
| MarketIngestion.Api | .NET 10 | **.NET 8** (`net8.0`) |
| Broker.Service | .NET 10 | **.NET 8** (`net8.0`) |
| AppHost | .NET 10 | .NET 10 (`net10.0`) ✅ |
| Server | .NET 10 | .NET 10 (`net10.0`) ✅ |

**Risk Level:** HIGH  
**Impact:** Confusion for developers, potential build issues, incorrect dependency expectations

**Recommendation:**
1. Update all projects to consistently target .NET 10, OR
2. Update documentation to reflect the actual mixed framework situation with clear explanation

---

### 1.2 Aspire Version Discrepancy

**Documentation Claims:**
- HIGH_LEVEL_DESIGN.md states "Aspire 9.5.2"
- ADR-003 references "Aspire 9.5.2 requires specific .NET versions"

**Actual Implementation:**
- AppHost uses `Aspire.AppHost.Sdk/13.1.0`
- Package references use Aspire packages version `13.1.0`

**Risk Level:** MEDIUM  
**Impact:** Documentation is significantly outdated; version 13.1.0 represents a major version jump

**Recommendation:**
Update all documentation references from "Aspire 9.5.2" to "Aspire 13.1.0"

---

### 1.3 Redis Configuration Naming Inconsistency

**In AppHost.cs:**
```csharp
var cache = builder.AddRedis("cache")
```

**In Services (MarketIngestion.Api, Broker.Service):**
```csharp
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
```

**Gap:** The Aspire resource is named "cache" but services expect configuration at `Redis:ConnectionString`. This works only because of the hardcoded fallback, not through proper Aspire service discovery.

**Risk Level:** MEDIUM  
**Impact:** Services not properly leveraging Aspire service discovery; inconsistent configuration

**Recommendation:**
1. Use Aspire's service discovery mechanism properly
2. Align naming between Aspire resource and configuration keys

---

### 1.4 Server Project - Undocumented Component

**Documentation:** No mention of `stock-market-simulator.Server` project anywhere in architecture docs

**Implementation:** The project exists with:
- A WeatherForecast endpoint (template code)
- OpenAPI configuration
- Output caching with Redis
- OpenTelemetry instrumentation

**Gap:** This project appears to be leftover template code that serves no purpose in the documented architecture

**Risk Level:** LOW  
**Impact:** Confusing codebase, unused code, potential maintenance burden

**Recommendation:**
1. Remove the Server project if not needed, OR
2. Document its purpose and integrate it into the architecture

---

## 2. Documentation Gaps

### 2.1 Frontend Documentation Incomplete

**HIGH_LEVEL_DESIGN.md Claims:**
- Web Application runs on port 5000
- Uses Material UI 5+
- Uses D3.js 7+
- Uses React 18+

**Actual Implementation:**
- React version: 19.2.0 (not 18+)
- Material UI version: 7.3.7 (not 5+)
- D3.js version: 7.9.0 (correctly documented)
- Frontend not mentioned in run-services.sh output

**Risk Level:** LOW  
**Impact:** Version information is outdated

**Recommendation:**
Update documentation to reflect actual package versions or use range notation like "19+" instead of specific versions

---

### 2.2 Missing Configuration Documentation

**Undocumented but Existing Configurations:**

| Service | Configuration | Source | Documentation Status |
|---------|---------------|--------|---------------------|
| MarketSimulator | Symbols, TickIntervalMs, Volatility, InitialPrice | appsettings.json | Partially documented in LLD |
| All Services | Redis:ConnectionString | appsettings.json | Not documented |
| Frontend | VITE_BROKER_URL | Environment | Not documented |

**Recommendation:**
Add a configuration reference section to the documentation

---

### 2.3 API Specification Gaps

**LOW_LEVEL_DESIGN.md Specifies:**
- POST /api/marketdata returns `{ messageId: string }`

**Actual Implementation:**
The implementation matches, but documentation lacks:
- OpenAPI/Swagger endpoints available in Development mode
- Health check endpoints (/health)
- Broker SSE endpoint format details

**Recommendation:**
Add complete API reference with all endpoints

---

## 3. Architectural Risks

### 3.1 Unbounded Channel Memory Risk

**Implementation (Broker.Service/Program.cs):**
```csharp
builder.Services.AddSingleton(Channel.CreateUnbounded<MarketDataEvent>(
    new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }));
```

**LLD Documentation States:**
> "Channel | Unbounded | Single writer, fast consumers"

**Risk:** Unbounded channels can lead to memory exhaustion if consumers are slow. The documentation acknowledges this but doesn't address it as a risk.

**Risk Level:** MEDIUM  
**Impact:** Potential OutOfMemory exception under high load or slow consumers

**Recommendation:**
1. Consider BoundedChannel with appropriate capacity
2. Document backpressure strategy
3. Add monitoring for channel depth

---

### 3.2 Single Consumer Name Collision

**Implementation (RedisStreamConsumer.cs):**
```csharp
private const string ConsumerName = "broker-1";
```

**LLD States:**
> "Consumer Name | broker-1 | Instance identifier"

**Risk:** If multiple Broker Service instances are deployed, they will all use the same consumer name, violating consumer group semantics.

**Risk Level:** HIGH (for production scaling)  
**Impact:** Message processing issues when horizontally scaling

**Recommendation:**
Generate unique consumer names (e.g., using hostname, process ID, or GUID)

---

### 3.3 No Data Validation on SSE Consumer

**Implementation (useMarketData.ts):**
```typescript
eventSource.addEventListener('marketdata', (e) => {
    const marketData: MarketDataEvent = JSON.parse(e.data);
    updateSecurity(marketData);
});
```

**Risk:** No validation of incoming data structure; assumes server always sends valid data

**Risk Level:** LOW  
**Impact:** Potential runtime errors if data format changes

**Recommendation:**
Add runtime validation (e.g., using Zod or similar library)

---

### 3.4 CORS Security Configuration

**Implementation (Broker.Service/Program.cs):**
```csharp
policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

**Documentation (HIGH_LEVEL_DESIGN.md):**
> "CORS enabled for localhost origins"

**Gap:** Implementation allows ANY origin, not just localhost

**Risk Level:** MEDIUM-HIGH  
**Impact:** Security vulnerability in non-development environments

**Recommendation:**
1. Update documentation to reflect actual behavior
2. Add environment-specific CORS configuration
3. Restrict origins in production

---

## 4. Best Practice Deviations

### 4.1 Lack of Unit Tests

**Current State:**
- `/tests` directory contains only performance tests
- No unit tests for any component
- LLD Section 10 documents testing approach but no actual tests exist

**Risk Level:** MEDIUM  
**Impact:** Code quality, regression risk, confidence in refactoring

**Recommendation:**
Implement unit tests for:
- ValidateMarketData function
- Security class change calculations
- PriceAggregator utility
- MarketDataEvent serialization

---

### 4.2 Missing Structured Error Types

**Implementation:** All services return generic Problem/ValidationProblem responses

**Best Practice:** Define domain-specific error types

**Recommendation:**
Create shared error contract types for consistent error handling across services

---

### 4.3 No Request Tracing Correlation

**Current State:** No correlation IDs or distributed tracing (despite OpenTelemetry packages in Server project)

**HLD States:**
> "OpenTelemetry: Aspire provides distributed tracing"

**Gap:** Tracing is not implemented in the core services (MarketSimulator, MarketIngestion.Api, Broker.Service)

**Recommendation:**
Add OpenTelemetry instrumentation to all services for proper distributed tracing

---

### 4.4 Hardcoded Magic Numbers

**Examples:**
```csharp
// ExchangeSimulator.cs
const int maxRetries = 3;
var delay = TimeSpan.FromMilliseconds(100);

// RedisStreamConsumer.cs  
count: 10  // Messages per read
TimeSpan.FromSeconds(5)  // Reconnect delay

// priceAggregator.ts
windowSizeMs: number = 60000
maxHistory: number = 30
```

**Recommendation:**
Extract to configuration with documented defaults

---

## 5. Documentation Quality Issues

### 5.1 README.md Outdated

**Issues:**
1. "Future Enhancements" lists React frontend, but it already exists and is functional
2. Web Application endpoint not listed in Service Endpoints
3. No mention of frontend in quick start guide

---

### 5.2 Inconsistent Terminology

| Term in Docs | Actual Name | Inconsistency |
|--------------|-------------|---------------|
| "Web Application" | "webfrontend" (Aspire), "frontend" (directory) | Multiple names |
| "Market Ingestion API" | "ingestion-api" (Aspire), "MarketIngestion.Api" (project) | Multiple naming conventions |
| "Broker Service" | "broker-service" (Aspire), "Broker.Service" (project) | Multiple naming conventions |

---

### 5.3 Missing Dependency Documentation

**Undocumented Dependencies:**
- date-fns (used in StockCard.tsx)
- @types/d3 (TypeScript definitions)
- Various ESLint packages

**Recommendation:**
Add a dependencies section or reference package.json in documentation

---

## 6. Positive Findings

### 6.1 Well-Aligned Patterns

| Pattern | Documentation | Implementation | Status |
|---------|--------------|----------------|--------|
| Event Streaming | Redis Streams | Redis Streams | ✅ Aligned |
| Server Push | SSE | SSE | ✅ Aligned |
| Background Processing | BackgroundService | BackgroundService | ✅ Aligned |
| Minimal API | Yes | Yes (MarketIngestion.Api) | ✅ Aligned |
| Consumer Groups | Yes | Yes | ✅ Aligned |

### 6.2 Correct Implementation of Core Features

- ✅ MarketDataEvent model matches documentation
- ✅ Security class with computed properties matches documentation
- ✅ Random walk price generation algorithm correctly implemented
- ✅ Validation rules match documentation
- ✅ SSE message format correct
- ✅ Retry logic with exponential backoff implemented as documented

### 6.3 Good Error Handling

- ✅ Comprehensive error handling in Redis operations
- ✅ Proper use of problem details for API errors
- ✅ Graceful reconnection handling in frontend

---

## 7. Prioritized Recommendations

### Immediate Actions (High Priority)

| # | Action | Impact | Effort |
|---|--------|--------|--------|
| 1 | Fix .NET version inconsistency (either update all to .NET 10 or document the mixed approach) | High | Medium |
| 2 | Update Aspire version references in documentation (9.5.2 → 13.1.0) | Medium | Low |
| 3 | Fix CORS configuration for production safety | High | Low |
| 4 | Generate unique consumer names for horizontal scaling | High | Low |

### Short-term Actions (Medium Priority)

| # | Action | Impact | Effort |
|---|--------|--------|--------|
| 5 | Update README.md with frontend instructions and correct endpoints | Medium | Low |
| 6 | Add configuration reference documentation | Medium | Low |
| 7 | Remove or document the Server project | Low | Low |
| 8 | Align Redis resource naming between Aspire and services | Medium | Medium |

### Long-term Actions (Lower Priority)

| # | Action | Impact | Effort |
|---|--------|--------|--------|
| 9 | Add unit tests for core components | Medium | High |
| 10 | Implement OpenTelemetry tracing in all services | Medium | Medium |
| 11 | Add bounded channel with backpressure handling | Medium | Medium |
| 12 | Extract hardcoded values to configuration | Low | Medium |
| 13 | Add runtime validation in frontend | Low | Low |

---

## 8. Conclusion

The Stock Market Simulator demonstrates a well-architected streaming data pipeline with good separation of concerns and appropriate technology choices. The core functionality is implemented correctly and aligns with the documented architecture patterns.

**Key Areas Requiring Attention:**

1. **Version Consistency**: The most critical issue is the .NET version mismatch between documentation (all .NET 10) and implementation (mixed .NET 8 and .NET 10)

2. **Documentation Currency**: Several version numbers and configuration details in documentation are outdated

3. **Production Readiness**: Several implementation details (CORS, consumer naming, unbounded channels) need attention before production deployment

4. **Missing Infrastructure**: Unit tests and proper distributed tracing are notably absent

The codebase shows good coding practices and follows the documented architectural decisions. With the recommended updates, this would serve as an excellent reference implementation for streaming architectures.

---

## Appendix A: Files Reviewed

### Documentation Files
- docs/architecture/README.md
- docs/architecture/HIGH_LEVEL_DESIGN.md
- docs/architecture/LOW_LEVEL_DESIGN.md
- docs/architecture/C4_DIAGRAMS.md
- docs/architecture/ADR/README.md
- docs/architecture/ADR/ADR-001-redis-streams.md
- docs/architecture/ADR/ADR-002-server-sent-events.md
- docs/architecture/ADR/ADR-003-dotnet-aspire.md
- docs/architecture/ADR/ADR-004-minimal-api.md
- docs/architecture/ADR/ADR-005-react-typescript.md

### Implementation Files
- src/Shared.Contracts/*.cs and *.csproj
- src/MarketSimulator/*.cs and *.csproj
- src/MarketIngestion.Api/*.cs and *.csproj
- src/Broker.Service/*.cs and *.csproj
- src/stock-market-simulator.AppHost/*.cs and *.csproj
- src/stock-market-simulator.Server/*.cs and *.csproj
- src/frontend/src/**/*.tsx, *.ts
- src/frontend/package.json
- run-services.sh
- README.md

---

## Appendix B: Reference Links

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Redis Streams](https://redis.io/docs/data-types/streams/)
- [Server-Sent Events (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
- [C4 Model](https://c4model.com/)
