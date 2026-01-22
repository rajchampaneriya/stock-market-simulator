# Task 05: Implement Market Simulator (Exchange Simulator)

## Objective
Create the exchange simulator that generates realistic market data ticks using a random walk algorithm.

## Prerequisites
- Task 02 completed (Shared.Contracts available)
- Task 04 completed (AppHost configured)

## Acceptance Criteria
- [ ] `ExchangeSimulator.cs` BackgroundService implemented
- [ ] Random walk price generation algorithm working
- [ ] HTTP POST to Ingestion API implemented
- [ ] PeriodicTimer used for async tick generation
- [ ] Configuration-driven symbols list
- [ ] Configurable tick interval (200-500ms)
- [ ] Proper error handling and logging
- [ ] Service starts and generates ticks continuously

## Implementation

### MarketSimulator/ExchangeSimulator.cs
```csharp
using Shared.Contracts;
using System.Net.Http.Json;

namespace MarketSimulator;

public class ExchangeSimulator : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeSimulator> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, decimal> _lastPrices = new();
    private readonly Random _random = new();

    public ExchangeSimulator(
        HttpClient httpClient,
        ILogger<ExchangeSimulator> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("Symbols").Get<string[]>() 
            ?? new[] { "NIFTY50", "BANKNIFTY" };
        var tickInterval = _configuration.GetValue<int>("TickIntervalMs", 300);
        var volatility = _configuration.GetValue<double>("Volatility", 0.5);

        // Initialize prices
        foreach (var symbol in symbols)
        {
            _lastPrices[symbol] = 1000m;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickInterval));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var symbol in symbols)
            {
                var marketData = GenerateTick(symbol, volatility);
                await SendToIngestion(marketData, stoppingToken);
            }
        }
    }

    private MarketDataEvent GenerateTick(string symbol, double volatility)
    {
        var lastPrice = _lastPrices[symbol];
        var change = (decimal)(_random.NextDouble() - 0.5) * 2 * (decimal)volatility;
        var newPrice = lastPrice + change;
        
        _lastPrices[symbol] = newPrice;

        return new MarketDataEvent
        {
            Symbol = symbol,
            Price = Math.Round(newPrice, 2),
            Volume = _random.Next(100, 10000),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task SendToIngestion(MarketDataEvent data, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/marketdata", data, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send market data for {Symbol}", data.Symbol);
        }
    }
}
```

### MarketSimulator/Program.cs
```csharp
using MarketSimulator;

var builder = Host.CreateApplicationBuilder(args);

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

### MarketSimulator/appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MarketSimulator": "Debug"
    }
  },
  "IngestionApiUrl": "http://localhost:5001",
  "Symbols": ["NIFTY50", "BANKNIFTY", "RELIANCE", "TCS"],
  "TickIntervalMs": 300,
  "Volatility": 0.5
}
```

## Verification
1. Run the simulator standalone:
   ```bash
   dotnet run --project src/MarketSimulator
   ```
2. Check logs for tick generation
3. Verify HTTP POST attempts (will fail until Ingestion API is ready)

## Related Sections
- Technical Approach Section 5.2 (Market Simulator)
- Technical Approach Section 6.1 (Configuration)
- Technical Approach Section 7 (Phase 2: Core Services)

## Estimated Effort
2-3 hours
