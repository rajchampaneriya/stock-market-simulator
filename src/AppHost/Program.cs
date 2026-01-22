// Configure the Aspire AppHost for stock market simulator
// Orchestrates Redis, MarketIngestion API, Broker Service, and Market Simulator
var builder = DistributedApplication.CreateBuilder(args);

// Redis cache for market data distribution
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

// Market Ingestion API - receives market data from simulator
var ingestion = builder.AddProject("ingestion-api", "../MarketIngestion.Api/MarketIngestion.Api.csproj")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5001, name: "ingestion-http");

// Broker Service - consumes market data from Redis and exposes via SSE
var broker = builder.AddProject("broker-service", "../Broker.Service/Broker.Service.csproj")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5002, name: "broker-http");

// Market Simulator - generates synthetic market data
builder.AddProject("market-simulator", "../MarketSimulator/MarketSimulator.csproj")
    .WithReference(ingestion)
    .WithEnvironment("IngestionApiUrl", ingestion.GetEndpoint("ingestion-http"));

builder.Build().Run();
