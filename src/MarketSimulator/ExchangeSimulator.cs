using Shared.Contracts;
using System.Net.Http.Json;

namespace MarketSimulator;

public class ExchangeSimulator : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeSimulator> _logger;
    private readonly IConfiguration _configuration;
    private readonly RealisticPriceEngine _priceEngine = new();

    /// <summary>
    /// Per-symbol profiles — different securities behave differently.
    /// </summary>
    private static readonly Dictionary<string, SymbolProfile> DefaultProfiles = new()
    {
        ["NIFTY50"]   = new() { Symbol = "NIFTY50",   BasePrice = 1000m, AnnualisedVolatility = 0.14, MeanReversionStrength = 0.0015, BaseVolume = 6000 },
        ["BANKNIFTY"] = new() { Symbol = "BANKNIFTY", BasePrice = 1000m, AnnualisedVolatility = 0.18, MeanReversionStrength = 0.0012, BaseVolume = 5500 },
        ["RELIANCE"]  = new() { Symbol = "RELIANCE",  BasePrice = 1000m, AnnualisedVolatility = 0.20, MeanReversionStrength = 0.0020, BaseVolume = 8000 },
        ["TCS"]       = new() { Symbol = "TCS",       BasePrice = 1000m, AnnualisedVolatility = 0.16, MeanReversionStrength = 0.0018, BaseVolume = 4500 },
    };

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
        var initialPrice = _configuration.GetValue<decimal>("InitialPrice", 1000m);

        // Register each symbol with the realistic price engine
        foreach (var symbol in symbols)
        {
            if (DefaultProfiles.TryGetValue(symbol, out var profile))
            {
                // Override base price from config if provided
                var adjusted = profile with { BasePrice = initialPrice };
                _priceEngine.RegisterSymbol(adjusted);
            }
            else
            {
                // Unknown symbol — use sensible defaults
                _priceEngine.RegisterSymbol(new SymbolProfile
                {
                    Symbol = symbol,
                    BasePrice = initialPrice,
                    AnnualisedVolatility = 0.16,
                    MeanReversionStrength = 0.0015,
                    BaseVolume = 5000,
                });
            }
        }

        _logger.LogInformation(
            "Exchange Simulator starting with {SymbolCount} symbols, tick interval {TickInterval}ms, realistic price engine",
            symbols.Length, tickInterval);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickInterval));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var symbol in symbols)
            {
                var marketData = GenerateTick(symbol);
                await SendToIngestion(marketData, stoppingToken);
            }
        }
    }

    private MarketDataEvent GenerateTick(string symbol)
    {
        var (price, volume) = _priceEngine.NextTick(symbol);

        return new MarketDataEvent
        {
            Symbol = symbol,
            Price = price,
            Volume = volume,
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
