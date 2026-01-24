using Broker.Service;
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Redis connection using Aspire service discovery
// The "cache" name matches the resource name in AppHost
builder.AddRedisClient("cache");

// Channel for broadcasting with bounded capacity to prevent memory issues
var channelCapacity = builder.Configuration.GetValue<int>("Channel:Capacity", 1000);
builder.Services.AddSingleton(Channel.CreateBounded<MarketDataEvent>(
    new BoundedChannelOptions(channelCapacity) 
    { 
        SingleReader = false, 
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.DropOldest // Drop oldest messages on backpressure
    }));

// Background consumer
builder.Services.AddHostedService<RedisStreamConsumer>();

// Health checks
var redisConnection = builder.Configuration.GetConnectionString("cache") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection);

// CORS for browser access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins for Aspire-generated URLs
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // In production, use strict origin configuration
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:5000", "http://localhost:5173" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Broker.Service",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddRedisInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
