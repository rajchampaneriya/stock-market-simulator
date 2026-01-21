# PRD Refinement Strategy

## Goal
Align prd.md with objective.md to ensure consistency, clarity, and engineering readiness.

## Key Changes Required

### 1. Document Metadata & References
- Add version and date
- Add reference to objective.md
- Clarify this is the detailed PRD based on the objective

### 2. Technology Stack Alignment
- **Add**: Explicit mention of Redis Streams as the streaming layer
- **Clarify**: .NET Aspire version consistency
- **Keep**: ReactJS, TypeScript, SSE, Material UI, d3js

### 3. Architecture Alignment
- **Rename components** to match objective.md:
  - "Exchange Simulator" → Keep but clarify it's "Market Data Generator"
  - "Market Data Gateway" → "Market Ingestion Service"
  - Simplify broker architecture to single "Broker Service" initially
  - Add "Analytics Engine" as optional component

### 4. Non-Functional Requirements
- **Update Latency**: Change from < 100ms to < 300ms
- **Update Scalability**: Change from 100 concurrent to 10k+ simulated users
- **Add**: Local-first deployment emphasis
- **Add**: Cost consideration (local-first)

### 5. Project Structure
- **Replace** current structure with Aspire-based structure:
```
/src
 ├── MarketSimulator (Exchange Simulator)
 ├── MarketIngestion.Api (Ingestion Service)
 ├── Broker.Service (Broker API)
 ├── WebApp (Web UI)
 ├── Shared.Contracts (Shared models)
 └── AppHost (Aspire orchestration)
```

### 6. Scope Simplification
- Emphasize "minimal but realistic prototype"
- Move multiple broker types to Future Enhancements
- Keep core flow simple: Exchange → Ingestion → Stream → Broker → Web UI
- Focus on demonstrating concepts, not production system

### 7. Data Flow Consistency
- Align data flow diagram with objective.md
- Use consistent terminology throughout

## Changes NOT to Make
- Don't remove valuable technical details
- Don't remove data models (they're useful for engineering)
- Don't remove testing strategy (it's good PM practice)
- Keep deployment options (adds value)

## Document Structure (Refined)
1. Introduction (with reference to objective.md)
2. System Overview (aligned with objective.md architecture)
3. Functional Requirements (simplified, core flow focused)
4. Non-Functional Requirements (aligned metrics)
5. Technical Specifications (detailed, engineering-ready)
6. Component Descriptions (aligned terminology)
7. Data Flow Diagrams (consistent with objective.md)
8. Implementation Requirements (Aspire-based structure)
9. Deployment Requirements
10. Future Enhancements (move complexity here)
11. Conclusion

