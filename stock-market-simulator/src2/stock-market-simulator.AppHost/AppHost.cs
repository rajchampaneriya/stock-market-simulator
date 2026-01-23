var builder = DistributedApplication.CreateBuilder(args);

// Redis for message streaming
var cache = builder.AddRedis("cache")    
    .WithDataVolume()
    .WithRedisCommander();

// Market Ingestion API - receives market data and publishes to Redis
var ingestionApi = builder.AddProject<Projects.MarketIngestion_Api>("ingestion-api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Market Simulator - generates fake market data
var marketSimulator = builder.AddProject<Projects.MarketSimulator>("market-simulator")
    .WithReference(ingestionApi)
    .WaitFor(ingestionApi);

// Broker Service - consumes from Redis and provides SSE endpoint
var brokerService = builder.AddProject<Projects.Broker_Service>("broker-service")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Frontend - React app
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_BROKER_URL", brokerService.GetEndpoint("http"))
    .WaitFor(brokerService);

builder.Build().Run();
