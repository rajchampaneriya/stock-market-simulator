# Task 16: Documentation and Deployment Guide

## Objective
Create comprehensive documentation for running, testing, and deploying the system.

## Prerequisites
- All implementation tasks completed (Tasks 01-15)

## Acceptance Criteria
- [ ] README.md updated with project overview
- [ ] SETUP.md created with detailed setup instructions
- [ ] ARCHITECTURE.md created with system design
- [ ] API documentation generated (Swagger)
- [ ] Deployment guide for local and Docker
- [ ] Troubleshooting guide created
- [ ] Code comments for complex logic
- [ ] Architecture diagrams exported
- [ ] Demo video or screenshots included

## Documentation Structure

### 1. Root README.md
```markdown
# Stock Market Simulator

Real-time stock market data streaming system built with .NET Aspire, Redis Streams, and React.

## Features
- Real-time market data streaming
- Server-Sent Events (SSE) for browser push
- Redis Streams for event distribution
- Material UI components
- Candlestick charts with d3.js
- OpenTelemetry observability

## Quick Start
\`\`\`bash
# Prerequisites
- .NET 10 SDK
- Node.js 18+
- Docker (for Redis)

# Run
cd src/AppHost
dotnet run

# Access
- Dashboard: http://localhost:15888
- Web UI: http://localhost:5000
\`\`\`

## Documentation
- [Setup Guide](docs/SETUP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [API Reference](docs/API.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

## License
MIT
```

### 2. docs/SETUP.md
```markdown
# Setup Guide

## Prerequisites
1. .NET 10 SDK
2. .NET Aspire workload: `dotnet workload install aspire`
3. Node.js 18+ and npm
4. Docker Desktop
5. Redis Commander (optional)

## Installation

### 1. Clone Repository
\`\`\`bash
git clone <repository-url>
cd stock-market-simulator
\`\`\`

### 2. Restore Dependencies
\`\`\`bash
dotnet restore
cd src/WebApp && npm install
\`\`\`

### 3. Run via Aspire
\`\`\`bash
cd src/AppHost
dotnet run
\`\`\`

### 4. Verify Services
- Aspire Dashboard: http://localhost:15888
- Web UI: http://localhost:5000
- Ingestion API: http://localhost:5001
- Broker Service: http://localhost:5002

## Configuration
[Details on appsettings.json configuration]

## Common Issues
[Troubleshooting section]
```

### 3. docs/ARCHITECTURE.md
```markdown
# System Architecture

## Overview
The Stock Market Simulator is an event-driven system that demonstrates real-time market data flow.

## Components
[Detailed description of each component]

## Data Flow
[Sequence diagram and explanation]

## Technology Decisions
[Rationale for Redis Streams, SSE, etc.]

## Deployment Options
[Local, Docker, Cloud]
```

### 4. docs/API.md
```markdown
# API Reference

## Market Ingestion API

### POST /api/marketdata
Publish market data event

**Request:**
\`\`\`json
{
  "symbol": "NIFTY50",
  "price": 1000.50,
  "volume": 5000,
  "timestamp": "2026-01-22T00:00:00Z"
}
\`\`\`

**Response:**
\`\`\`json
{
  "messageId": "1234567890-0"
}
\`\`\`

## Broker Service API

### GET /api/marketdata/stream
Server-Sent Events endpoint for real-time market data

**Events:**
- `connected`: Connection established
- `marketdata`: Market data update
- `heartbeat`: Keep-alive ping
```

### 5. docs/TROUBLESHOOTING.md
```markdown
# Troubleshooting Guide

## Service Won't Start

### Redis Connection Failed
**Symptom:** "Unable to connect to Redis"
**Solution:**
1. Check Docker is running
2. Verify Redis container is up: `docker ps`
3. Test connection: `redis-cli ping`

### Port Already in Use
**Symptom:** "Address already in use"
**Solution:**
1. Find process: `lsof -i :5001`
2. Kill process or change port in appsettings.json

## No Data in UI

### Check Data Flow
\`\`\`bash
# 1. Verify simulator is running
# Check logs for POST requests

# 2. Verify ingestion API
curl http://localhost:5001/health

# 3. Check Redis stream
redis-cli XLEN marketdata:stream

# 4. Verify broker consumer
redis-cli XINFO GROUPS marketdata:stream
\`\`\`

## Performance Issues
[Solutions for high latency, memory leaks, etc.]
```

## Swagger Documentation

### Enable in Ingestion API
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Market Ingestion API",
        Version = "v1",
        Description = "API for ingesting market data into the streaming system"
    });
});

// In app configuration
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Ingestion API v1");
});
```

### Enable in Broker Service
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Broker Service API",
        Version = "v1",
        Description = "API for streaming market data to clients"
    });
});
```

## Docker Compose (Optional)

### docker-compose.yml
```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  ingestion-api:
    build:
      context: .
      dockerfile: src/MarketIngestion.Api/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis

  broker-service:
    build:
      context: .
      dockerfile: src/Broker.Service/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis

  market-simulator:
    build:
      context: .
      dockerfile: src/MarketSimulator/Dockerfile
    environment:
      - IngestionApiUrl=http://ingestion-api:8080
    depends_on:
      - ingestion-api

volumes:
  redis-data:
```

## Code Comments

Add XML documentation to all public APIs:
```csharp
/// <summary>
/// Processes incoming market data and publishes to Redis Streams.
/// </summary>
/// <param name="marketData">The market data event to process</param>
/// <param name="redis">Redis connection multiplexer</param>
/// <param name="logger">Logger instance</param>
/// <returns>ActionResult with message ID on success</returns>
```

## Verification

### 1. Documentation Review
- [ ] All setup steps work on clean machine
- [ ] API examples are accurate
- [ ] Troubleshooting covers common issues
- [ ] Architecture diagrams are clear

### 2. Swagger UI Test
- Navigate to http://localhost:5001/swagger
- Try API endpoints via Swagger UI
- Verify examples work

### 3. README Test
- Clone repo to new directory
- Follow README quick start
- Verify system runs

## Related Sections
- Technical Approach Section 9 (Deployment)
- Technical Approach Section 11 (References)

## Estimated Effort
4-5 hours
