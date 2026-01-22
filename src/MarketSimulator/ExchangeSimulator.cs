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

        _logger.LogInformation("Exchange Simulator starting with {SymbolCount} symbols, tick interval {TickInterval}ms", 
            symbols.Length, tickInterval);

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
            
            _logger.LogDebug("Sent market data for {Symbol} at price {Price}", 
                data.Symbol, data.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send market data for {Symbol}", data.Symbol);
        }
    }
}
