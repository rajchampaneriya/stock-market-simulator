# Task 04: Configure All Services in Aspire AppHost

## Objective
Complete the Aspire AppHost configuration by adding all service projects with proper references and endpoints.

## Prerequisites
- Task 01 completed (all projects created)
- Task 03 completed (Redis configured)

## Acceptance Criteria
- [ ] Market Ingestion API added with Redis reference
- [ ] Broker Service added with Redis reference
- [ ] Market Simulator added with ingestion API reference
- [ ] All services have proper HTTP endpoints configured
- [ ] Service discovery configured correctly
- [ ] All services start successfully via AppHost
- [ ] Aspire dashboard shows all services as healthy

## Implementation

### AppHost/Program.cs (Complete)
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

// Add Market Ingestion API
var ingestion = builder.AddProject<Projects.MarketIngestion_Api>("ingestion-api")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5001, name: "http");

// Add Broker Service
var broker = builder.AddProject<Projects.Broker_Service>("broker-service")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5002, name: "http");

// Add Market Simulator
builder.AddProject<Projects.MarketSimulator>("market-simulator")
    .WithReference(ingestion)
    .WithEnvironment("IngestionApiUrl", ingestion.GetEndpoint("http"));

builder.Build().Run();
```

## Verification
1. Build entire solution: `dotnet build`
2. Run AppHost: `dotnet run --project src/AppHost`
3. Open Aspire Dashboard: http://localhost:15888
4. Verify all services are listed and healthy:
   - redis
   - ingestion-api (port 5001)
   - broker-service (port 5002)
   - market-simulator
5. Check traces are being collected

## Related Sections
- Technical Approach Section 5.6 (Aspire AppHost)
- Technical Approach Section 3.1 (Architecture Diagram)

## Estimated Effort
1-2 hours
