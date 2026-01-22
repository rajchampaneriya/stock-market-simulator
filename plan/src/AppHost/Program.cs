var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

var ingestion = builder.AddProject("ingestion-api", "../MarketIngestion.Api/MarketIngestion.Api.csproj")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5001, name: "http");

var broker = builder.AddProject("broker-service", "../Broker.Service/Broker.Service.csproj")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5002, name: "http");

builder.AddProject("market-simulator", "../MarketSimulator/MarketSimulator.csproj")
    .WithReference(ingestion)
    .WithEnvironment("IngestionApiUrl", ingestion.GetEndpoint("http"));

builder.Build().Run();
