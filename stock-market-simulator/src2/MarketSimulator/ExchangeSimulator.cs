using Shared.Contracts;
using System.Net.Http.Json;

namespace MarketSimulator;

public class ExchangeSimulator : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeSimulator> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, decimal> _lastPrices = new();
    private readonly Random _random = new();

    public ExchangeSimulator(
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeSimulator> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("Symbols").Get<string[]>() 
            ?? new[] { "NIFTY50", "BANKNIFTY" };
        var tickInterval = _configuration.GetValue<int>("TickIntervalMs", 300);
        var volatility = _configuration.GetValue<double>("Volatility", 0.5);
        var initialPrice = _configuration.GetValue<decimal>("InitialPrice", 1000m);

        _logger.LogInformation("Exchange Simulator starting with {SymbolCount} symbols, tick interval {TickInterval}ms", 
            symbols.Length, tickInterval);

        // Initialize prices
        foreach (var symbol in symbols)
        {
            _lastPrices[symbol] = initialPrice;
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
        
        // Ensure price stays positive
        newPrice = Math.Max(0.01m, newPrice);
        
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
        const int maxRetries = 3;
        var retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(100);

        while (retryCount < maxRetries)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("IngestionApi");
                var response = await httpClient.PostAsJsonAsync("/api/marketdata", data, ct);
                response.EnsureSuccessStatusCode();
                
                _logger.LogDebug("Sent market data for {Symbol} at price {Price}", 
                    data.Symbol, data.Price);
                return;
            }
            catch (HttpRequestException ex)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Failed to send {Symbol} (attempt {Attempt}/{Max})", 
                    data.Symbol, retryCount, maxRetries);
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("Max retries reached for {Symbol}, data may be lost", data.Symbol);
                    return;
                }
                
                await Task.Delay(delay, ct);
                delay *= 2;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request timeout for {Symbol}", data.Symbol);
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("Max retries reached for {Symbol} due to timeouts", data.Symbol);
                    return;
                }
                
                await Task.Delay(delay, ct);
                delay *= 2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending market data for {Symbol}", data.Symbol);
                return;
            }
        }
    }
}
