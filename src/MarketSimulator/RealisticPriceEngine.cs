namespace MarketSimulator;

/// <summary>
/// Per-symbol configuration for the realistic price engine.
/// Controls trend, volatility, mean reversion, and volume characteristics.
/// </summary>
public record SymbolProfile
{
    public string Symbol { get; init; } = string.Empty;
    
    /// <summary>Base price the symbol mean-reverts toward.</summary>
    public decimal BasePrice { get; init; } = 1000m;
    
    /// <summary>Annualised volatility (e.g., 0.15 = 15%).</summary>
    public double AnnualisedVolatility { get; init; } = 0.15;
    
    /// <summary>Mean reversion speed — higher = faster pull back to base price.</summary>
    public double MeanReversionStrength { get; init; } = 0.002;
    
    /// <summary>Average volume per tick.</summary>
    public int BaseVolume { get; init; } = 5000;
    
    /// <summary>Minimum price floor.</summary>
    public decimal MinPrice { get; init; } = 100m;
}

/// <summary>
/// Generates realistic tick-level price data using:
///  • Geometric Brownian Motion (GBM) as the core random walk
///  • Ornstein–Uhlenbeck mean reversion toward a base price
///  • GARCH-inspired volatility clustering  
///  • Periodic micro-trends that create recognizable chart patterns
///  • Intraday volume variation
/// </summary>
public class RealisticPriceEngine
{
    private readonly Random _rng;
    
    // --- Per-symbol state ---
    private readonly Dictionary<string, SymbolState> _states = new();
    private readonly Dictionary<string, SymbolProfile> _profiles = new();
    
    // --- Volatility clustering (GARCH-like) ---
    private const double GarchOmega  = 0.00001;   // Base variance
    private const double GarchAlpha  = 0.10;       // Shock sensitivity
    private const double GarchBeta   = 0.85;       // Persistence
    
    // --- Micro-trend ---
    private const int MinTrendTicks  = 40;   // ~12 seconds at 300ms
    private const int MaxTrendTicks  = 200;  // ~60 seconds at 300ms
    private const double TrendDriftRange = 0.0003; // Max drift per tick during a trend

    public RealisticPriceEngine(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Registers a symbol with its profile. Must be called before GenerateTick.
    /// </summary>
    public void RegisterSymbol(SymbolProfile profile)
    {
        _profiles[profile.Symbol] = profile;
        _states[profile.Symbol] = new SymbolState
        {
            CurrentPrice = profile.BasePrice,
            ConditionalVariance = TickVariance(profile.AnnualisedVolatility),
            TrendDrift = 0,
            TicksRemainingInTrend = RandomTrendLength(),
        };
    }

    /// <summary>
    /// Produces the next realistic tick for the given symbol.
    /// </summary>
    public (decimal Price, long Volume) NextTick(string symbol)
    {
        if (!_states.TryGetValue(symbol, out var state) || !_profiles.TryGetValue(symbol, out var profile))
            throw new InvalidOperationException($"Symbol '{symbol}' not registered.");

        // 1. Standard-normal shock  
        var z = NormalSample();

        // 2. GARCH conditional volatility  
        var sigma = Math.Sqrt(state.ConditionalVariance);
        var shock = sigma * z;

        // Update GARCH variance for next tick  
        state.ConditionalVariance = GarchOmega
            + GarchAlpha * (shock * shock)
            + GarchBeta * state.ConditionalVariance;

        // 3. Mean reversion (Ornstein–Uhlenbeck pull)  
        var deviation = (double)(state.CurrentPrice - profile.BasePrice) / (double)profile.BasePrice;
        var meanRevPull = -profile.MeanReversionStrength * deviation;

        // 4. Micro-trend drift  
        state.TicksRemainingInTrend--;
        if (state.TicksRemainingInTrend <= 0)
        {
            // Start a new micro-trend
            state.TrendDrift = (_rng.NextDouble() - 0.5) * 2.0 * TrendDriftRange;
            state.TicksRemainingInTrend = RandomTrendLength();
        }

        // 5. Combine components into a log-return  
        var logReturn = meanRevPull + state.TrendDrift + shock;

        // 6. Apply to price via GBM (multiplicative)  
        var newPrice = state.CurrentPrice * (decimal)Math.Exp(logReturn);
        newPrice = Math.Max(profile.MinPrice, Math.Round(newPrice, 2));
        state.CurrentPrice = newPrice;

        // 7. Volume — base ± 50% random variation + volatility influence  
        var volMultiplier = 1.0 + Math.Abs(z) * 0.5;   // higher vol → higher volume
        var volumeNoise = 0.5 + _rng.NextDouble();       // 0.5x – 1.5x
        var volume = (long)(profile.BaseVolume * volumeNoise * volMultiplier);
        volume = Math.Max(100, volume);

        return (newPrice, volume);
    }

    // --- Helpers ---

    /// <summary>Convert annualised vol to per-tick variance (≈ 300 ms ticks).</summary>
    private static double TickVariance(double annualVol)
    {
        // ~7.5 hours trading × 3600s / 0.3s = 90 000 ticks/day, 252 days/year
        const double ticksPerYear = 90_000.0 * 252.0;
        var tickVol = annualVol / Math.Sqrt(ticksPerYear);
        return tickVol * tickVol;
    }

    private int RandomTrendLength() => _rng.Next(MinTrendTicks, MaxTrendTicks);

    /// <summary>Box–Muller transform for standard-normal samples.</summary>
    private double NormalSample()
    {
        var u1 = 1.0 - _rng.NextDouble(); // (0,1]
        var u2 = _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private class SymbolState
    {
        public decimal CurrentPrice;
        public double ConditionalVariance;
        public double TrendDrift;
        public int TicksRemainingInTrend;
    }
}
