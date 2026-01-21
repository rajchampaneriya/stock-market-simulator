# Technical Approach Reconciliation Plan
## Stock Market Data Streaming System

**Date:** January 21, 2026  
**Purpose:** Reconcile draft-technical-approach.md with objective.md and prd.md requirements  
**Status:** Planning Complete - Ready for Implementation

---

## 1. Executive Summary

This document outlines the specific changes required to align draft-technical-approach.md with the agreed objectives and PRD requirements. The current draft introduces unnecessary complexity and deviates from the simple, focused architecture defined in the PRD.

**Reconciliation Strategy:** Complete restructure of draft-technical-approach.md to match PRD specifications exactly.

---

## 2. Gap Analysis Summary

### 2.1 What to Remove from Draft
❌ Custom IMarketDataPublisher abstraction  
❌ Custom MarketDataGateway with channels  
❌ IMarketDataSubscriber pattern  
❌ Complex data model with bid/ask spreads  
❌ MarketDataEventType enum  
❌ Custom timer-based generation in constructor  
❌ Inline HTML/CSS/JS prototype (replace with React approach)

### 2.2 What to Add to Draft
✅ Aspire AppHost orchestration setup  
✅ Redis Streams implementation  
✅ Market Ingestion Service (HTTP Minimal API)  
✅ Broker Service with SSE support  
✅ Simple MarketDataEvent model  
✅ HTTP client for simulator to ingestion API  
✅ Redis Streams consumer for broker  
✅ React + TypeScript + Material UI approach  
✅ OpenTelemetry integration  

### 2.3 What to Keep from Draft
✅ Overall structure approach (component-by-component)  
✅ BackgroundService pattern for simulator  
✅ Price generation logic (simplified)  
✅ Code structure with comments  

---

## 3. Updated Technical Approach Structure

### 3.1 New Document Outline

```
1. Introduction & Overview
2. Technology Stack
3. Project Structure (Aspire-based)
4. Shared Contracts
   4.1 MarketDataEvent (simplified)
   4.2 Security (price cache model)
5. Component 1: Market Data Generator (Exchange Simulator)
   5.1 Implementation
   5.2 Configuration
6. Component 2: Market Ingestion Service
   6.1 HTTP API endpoints
   6.2 Redis Streams publishing
   6.3 Validation
7. Component 3: Redis Streams Configuration
   7.1 Stream setup
   7.2 Consumer groups
   7.3 Retention policy
8. Component 4: Broker Service
   8.1 Redis Streams consumer
   8.2 Price caching
   8.3 SSE implementation
   8.4 Connection management
9. Component 5: Web UI (React)
   9.1 Project setup
   9.2 SSE client implementation
   9.3 State management
   9.4 Material UI components
   9.5 d3.js candlestick chart
10. Component 6: Aspire AppHost
    10.1 Service orchestration
    10.2 Service discovery
    10.3 Redis integration
11. Configuration Files
    11.1 appsettings.json examples
    11.2 Docker Compose (optional)
12. Testing Approach
13. Deployment Guide
14. Appendix: Code Snippets
```

---

## 4. Detailed Component Specifications

### 4.1 Shared Contracts (Shared.Contracts project)

#### MarketDataEvent.cs (Simplified for MVP)
```csharp
namespace Shared.Contracts;

public record MarketDataEvent
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
}
```

**Changes from Draft:**
- ❌ Removed: BidPrice, AskPrice, BidSize, AskSize (not needed for MVP)
- ❌ Removed: EventType enum (simplified)
- ❌ Removed: Data dictionary (use typed properties)
- ✅ Added: Record type for immutability
- ✅ Simplified: Only essential fields

#### Security.cs (For Broker Cache)
```csharp
namespace Shared.Contracts;

public class Security
{
    public string Symbol { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdate { get; set; }
    
    public decimal Change => LastPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 
        ? (Change / PreviousPrice) * 100 
        : 0;
}
```

**Matches PRD Section 5.1.2 exactly**

---

### 4.2 Market Data Generator (MarketSimulator project)

#### ExchangeSimulator.cs
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace MarketSimulator;

public class ExchangeSimulator : BackgroundService
{
    private readonly ILogger<ExchangeSimulator> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, decimal> _basePrices = new();
    private readonly Random _random = new();
    
    // Configuration
    private string[] Symbols => _configuration
        .GetSection("Symbols")
        .Get<string[]>() ?? ["NIFTY50", "BANKNIFTY", "RELIANCE", "TCS"];
    
    private int TickIntervalMs => _configuration
        .GetValue<int>("TickIntervalMs", 300);

    public ExchangeSimulator(
        ILogger<ExchangeSimulator> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        
        // Initialize base prices
        InitializeBasePrices();
    }

    private void InitializeBasePrices()
    {
        // Set realistic base prices for Indian market symbols
        var defaults = new Dictionary<string, decimal>
        {
            ["NIFTY50"] = 22400.00m,
            ["BANKNIFTY"] = 48100.00m,
            ["RELIANCE"] = 2850.00m,
            ["TCS"] = 3950.00m
        };

        foreach (var symbol in Symbols)
        {
            _basePrices[symbol] = defaults.TryGetValue(symbol, out var price) 
                ? price 
                : 1000.00m;
        }
        
        _logger.LogInformation("Initialized {Count} symbols", _basePrices.Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Exchange Simulator started");
        
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));
        
        while (!stoppingToken.IsCancellationRequested 
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateAndPublishTicksAsync(stoppingToken);
        }
        
        _logger.LogInformation("Exchange Simulator stopped");
    }

    private async Task GenerateAndPublishTicksAsync(CancellationToken cancellationToken)
    {
        foreach (var (symbol, basePrice) in _basePrices)
        {
            // Generate realistic price movement (random walk)
            var priceChange = (decimal)(_random.NextDouble() - 0.5) * 10; // -5 to +5
            var newPrice = Math.Max(0.01m, basePrice + priceChange);
            
            // Update base price for next iteration
            _basePrices[symbol] = newPrice;
            
            // Generate volume
            var volume = _random.Next(100, 5000);
            
            // Create event
            var marketData = new MarketDataEvent
            {
                Symbol = symbol,
                Price = Math.Round(newPrice, 2),
                Volume = volume,
                Timestamp = DateTime.UtcNow
            };
            
            // Publish to Ingestion API
            await PublishAsync(marketData, cancellationToken);
        }
    }

    private async Task PublishAsync(
        MarketDataEvent marketData, 
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(marketData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                "/api/marketdata", 
                content, 
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to publish {Symbol}: {Status}", 
                    marketData.Symbol, 
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing market data for {Symbol}", 
                marketData.Symbol);
        }
    }
}
```

#### Program.cs (MarketSimulator)
```csharp
using MarketSimulator;

var builder = Host.CreateApplicationBuilder(args);

// Configure HttpClient for Ingestion API
builder.Services.AddHttpClient<ExchangeSimulator>(client =>
{
    var ingestionUrl = builder.Configuration["IngestionApiUrl"] 
        ?? "http://localhost:5001";
    client.BaseAddress = new Uri(ingestionUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<ExchangeSimulator>();

var host = builder.Build();
await host.RunAsync();
```

**Key Changes:**
- ✅ Uses HttpClient instead of custom IMarketDataPublisher
- ✅ Uses PeriodicTimer (modern .NET pattern)
- ✅ Simple price generation (random walk)
- ✅ Configuration-driven symbols
- ✅ Proper error handling
- ❌ Removed: Custom gateway, channels, abstractions

---

### 4.3 Market Ingestion Service (MarketIngestion.Api project)

#### Program.cs
```csharp
using StackExchange.Redis;
using System.Text.Json;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnection = configuration["Redis:ConnectionString"] 
        ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConnection);
});

// Add observability
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var app = builder.Build();

// Market data ingestion endpoint
app.MapPost("/api/marketdata", async (
    MarketDataEvent marketData,
    IConnectionMultiplexer redis,
    ILogger<Program> logger) =>
{
    try
    {
        // Validate
        if (string.IsNullOrWhiteSpace(marketData.Symbol))
        {
            return Results.BadRequest("Symbol is required");
        }
        
        if (marketData.Price <= 0)
        {
            return Results.BadRequest("Price must be positive");
        }
        
        // Publish to Redis Stream
        var db = redis.GetDatabase();
        var streamKey = "marketdata:stream";
        
        var fields = new NameValueEntry[]
        {
            new("symbol", marketData.Symbol),
            new("price", marketData.Price.ToString()),
            new("volume", marketData.Volume.ToString()),
            new("timestamp", marketData.Timestamp.ToString("O"))
        };
        
        var messageId = await db.StreamAddAsync(streamKey, fields);
        
        logger.LogDebug(
            "Published {Symbol} to stream: {MessageId}", 
            marketData.Symbol, 
            messageId);
        
        return Results.Ok(new { messageId = messageId.ToString() });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error ingesting market data");
        return Results.Problem("Failed to ingest market data");
    }
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

**Key Features:**
- ✅ Minimal API (modern .NET pattern)
- ✅ Redis Streams publishing (XADD)
- ✅ Input validation
- ✅ Error handling
- ✅ Health endpoint
- ✅ OpenTelemetry instrumentation

---

### 4.4 Broker Service (Broker.Service project)

#### RedisStreamConsumer.cs
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;

namespace Broker.Service;

public class RedisStreamConsumer : BackgroundService
{
    private readonly ILogger<RedisStreamConsumer> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly Channel<MarketDataEvent> _marketDataChannel;
    private readonly string _streamKey = "marketdata:stream";
    private readonly string _consumerGroup = "brokers";
    private readonly string _consumerName;

    public RedisStreamConsumer(
        ILogger<RedisStreamConsumer> logger,
        IConnectionMultiplexer redis,
        Channel<MarketDataEvent> marketDataChannel)
    {
        _logger = logger;
        _redis = redis;
        _marketDataChannel = marketDataChannel;
        _consumerName = $"broker-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis Stream Consumer started");
        
        var db = _redis.GetDatabase();
        
        // Create consumer group if it doesn't exist
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                _streamKey, 
                _consumerGroup, 
                StreamPosition.NewMessages);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists
            _logger.LogInformation("Consumer group already exists");
        }
        
        // Start consuming
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await db.StreamReadGroupAsync(
                    _streamKey,
                    _consumerGroup,
                    _consumerName,
                    ">",
                    count: 10);
                
                foreach (var message in messages)
                {
                    var marketData = ParseMarketData(message);
                    if (marketData != null)
                    {
                        await _marketDataChannel.Writer.WriteAsync(
                            marketData, 
                            stoppingToken);
                        
                        // Acknowledge message
                        await db.StreamAcknowledgeAsync(
                            _streamKey, 
                            _consumerGroup, 
                            message.Id);
                    }
                }
                
                // Delay if no messages
                if (!messages.Any())
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming from Redis Stream");
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        _logger.LogInformation("Redis Stream Consumer stopped");
    }

    private MarketDataEvent? ParseMarketData(StreamEntry entry)
    {
        try
        {
            var values = entry.Values.ToDictionary(
                x => x.Name.ToString(), 
                x => x.Value.ToString());
            
            return new MarketDataEvent
            {
                Symbol = values["symbol"],
                Price = decimal.Parse(values["price"]),
                Volume = long.Parse(values["volume"]),
                Timestamp = DateTime.Parse(values["timestamp"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse market data");
            return null;
        }
    }
}
```

#### SseController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;
using System.Text.Json;
using Shared.Contracts;

namespace Broker.Service.Controllers;

[ApiController]
[Route("api/stream")]
public class SseController : ControllerBase
{
    private readonly Channel<MarketDataEvent> _marketDataChannel;
    private readonly ILogger<SseController> _logger;

    public SseController(
        Channel<MarketDataEvent> marketDataChannel,
        ILogger<SseController> logger)
    {
        _marketDataChannel = marketDataChannel;
        _logger = logger;
    }

    [HttpGet("marketdata")]
    public async Task StreamMarketData(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        _logger.LogInformation("Client connected to SSE stream");
        
        try
        {
            // Send initial connection confirmation
            await SendSseEventAsync("connected", 
                new { message = "Connected to market data stream" });
            
            // Create a reader for this connection
            var reader = _marketDataChannel.Reader;
            
            // Send heartbeat and data
            using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            var heartbeatTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested 
                       && await heartbeatTimer.WaitForNextTickAsync(cancellationToken))
                {
                    await SendSseEventAsync("heartbeat", 
                        new { timestamp = DateTime.UtcNow });
                }
            }, cancellationToken);
            
            // Stream market data
            await foreach (var marketData in reader.ReadAllAsync(cancellationToken))
            {
                await SendSseEventAsync("marketdata", marketData);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected from SSE stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE stream");
        }
    }

    private async Task SendSseEventAsync(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: {eventType}\n");
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }
}
```

#### Program.cs (Broker.Service)
```csharp
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;
using Broker.Service;

var builder = WebApplication.CreateBuilder(args);

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnection = configuration["Redis:ConnectionString"] 
        ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(redisConnection);
});

// Add channel for market data distribution
builder.Services.AddSingleton(Channel.CreateUnbounded<MarketDataEvent>(
    new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = true
    }));

// Add Redis consumer
builder.Services.AddHostedService<RedisStreamConsumer>();

// Add controllers
builder.Services.AddControllers();

// Add observability
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation());

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

**Key Features:**
- ✅ Redis Streams consumer (XREADGROUP)
- ✅ Consumer group management
- ✅ SSE implementation
- ✅ Heartbeat support
- ✅ Connection management
- ✅ Channel for internal distribution
- ✅ Proper error handling

---

### 4.5 Web UI Approach (React + TypeScript)

#### Key Files Structure
```
WebApp/
├── public/
│   └── index.html
├── src/
│   ├── components/
│   │   ├── ConnectionStatus.tsx
│   │   ├── StockCard.tsx
│   │   ├── CandlestickChart.tsx
│   │   └── EventLog.tsx
│   ├── hooks/
│   │   └── useMarketData.ts
│   ├── types/
│   │   └── marketData.ts
│   ├── App.tsx
│   └── index.tsx
├── package.json
└── tsconfig.json
```

#### useMarketData.ts (Custom Hook)
```typescript
import { useState, useEffect } from 'react';

interface MarketDataEvent {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export const useMarketData = (brokerUrl: string) => {
  const [data, setData] = useState<Map<string, MarketDataEvent>>(new Map());
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const eventSource = new EventSource(`${brokerUrl}/api/stream/marketdata`);
    
    eventSource.onopen = () => {
      setConnected(true);
      setError(null);
    };
    
    eventSource.addEventListener('marketdata', (e) => {
      const marketData: MarketDataEvent = JSON.parse(e.data);
      setData(prev => new Map(prev).set(marketData.symbol, marketData));
    });
    
    eventSource.onerror = () => {
      setConnected(false);
      setError('Connection lost. Reconnecting...');
    };
    
    return () => {
      eventSource.close();
    };
  }, [brokerUrl]);

  return { data, connected, error };
};
```

**Note:** Full React implementation would include Material UI components and d3.js charts as per PRD section 6.5.

---

### 4.6 Aspire AppHost

#### Program.cs (AppHost project)
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add Market Ingestion API
var ingestion = builder.AddProject<Projects.MarketIngestion_Api>("ingestion-api")
    .WithReference(redis);

// Add Broker Service
var broker = builder.AddProject<Projects.Broker_Service>("broker-service")
    .WithReference(redis);

// Add Market Simulator
builder.AddProject<Projects.MarketSimulator>("market-simulator")
    .WithReference(ingestion);

// Add Web UI
builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(broker);

builder.Build().Run();
```

**Key Features:**
- ✅ Service orchestration
- ✅ Service discovery
- ✅ Redis integration
- ✅ Automatic configuration
- ✅ Observability dashboard

---

## 5. Implementation Checklist

### Phase 1: Foundation
- [ ] Create solution structure with Aspire template
- [ ] Create Shared.Contracts project with MarketDataEvent
- [ ] Setup AppHost with Redis reference
- [ ] Verify Aspire dashboard works

### Phase 2: Core Services
- [ ] Implement Market Simulator with HTTP client
- [ ] Implement Ingestion API with Redis Streams publishing
- [ ] Test simulator → ingestion → Redis flow

### Phase 3: Distribution
- [ ] Implement Broker Service Redis consumer
- [ ] Implement SSE endpoint in Broker
- [ ] Test Redis → Broker → SSE flow

### Phase 4: UI
- [ ] Create React app with TypeScript
- [ ] Implement SSE client hook
- [ ] Create basic UI with Material UI
- [ ] Test end-to-end flow

### Phase 5: Polish
- [ ] Add d3.js candlestick charts
- [ ] Add error handling and reconnection
- [ ] Performance testing
- [ ] Documentation

---

## 6. Success Criteria

### Technical Alignment
✅ Uses Redis Streams (not custom gateway)  
✅ Uses HTTP POST from simulator to ingestion  
✅ Uses Aspire orchestration  
✅ Uses SSE for browser push  
✅ Simple MarketDataEvent model  
✅ Matches PRD architecture exactly  

### Quality Criteria
✅ End-to-end latency < 300ms  
✅ Handles 10+ events/sec  
✅ Clean, maintainable code  
✅ Proper error handling  
✅ OpenTelemetry traces  

---

## 7. Next Actions

1. ✅ Assessment complete
2. ✅ Reconciliation plan complete
3. ⏳ Update draft-technical-approach.md with new structure
4. ⏳ Create before-you-build checklist
5. ⏳ Begin implementation

---

**Document Status:** Complete  
**Next Step:** Update draft-technical-approach.md
