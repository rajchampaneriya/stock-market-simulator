using Shared.Contracts;

namespace Broker.Service.Services;

/// <summary>
/// In-memory per-symbol ring buffer of completed 1-minute candlestick data.
///
/// Responsibilities:
///  1. Stores completed candles pushed from <see cref="CandlestickAggregator" />.
///  2. Seeds realistic historical candles on first request (lazy) using
///     a lightweight GBM random walk — matching the style of
///     <c>MarketSimulator.RealisticPriceEngine</c>.
///  3. Returns the full history for a symbol (newest last) so SSE can replay
///     it on client connect.
/// </summary>
public class CandlestickHistory
{
    private readonly int _maxCandles;
    private readonly int _seedCount;
    private readonly object _lock = new();

    // symbol → list of completed candles (oldest first)
    private readonly Dictionary<string, List<CandlestickEvent>> _history = new();
    private readonly HashSet<string> _seeded = new();

    /// <summary>Known symbols with their seed base prices.</summary>
    private static readonly Dictionary<string, SymbolSeedProfile> SeedProfiles = new()
    {
        ["NIFTY50"]   = new(1000m, 0.14),
        ["BANKNIFTY"] = new(1000m, 0.18),
        ["RELIANCE"]  = new(1000m, 0.20),
        ["TCS"]       = new(1000m, 0.16),
    };

    public CandlestickHistory(int maxCandles = 60, int seedCount = 30)
    {
        _maxCandles = maxCandles;
        _seedCount = seedCount;
    }

    /// <summary>
    /// Appends a completed candle for a symbol. Called by <see cref="CandlestickAggregator"/>
    /// whenever a candle is finalised.
    /// </summary>
    public void Push(CandlestickEvent candle)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(candle.Symbol, out var list))
            {
                list = new List<CandlestickEvent>();
                _history[candle.Symbol] = list;
            }

            // Avoid duplicates (same interval start)
            if (list.Count > 0 && list[^1].IntervalStart == candle.IntervalStart)
            {
                list[^1] = candle; // update in-place
            }
            else
            {
                list.Add(candle);
            }

            // Trim to max
            while (list.Count > _maxCandles)
            {
                list.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Returns all completed candles for a symbol (oldest first).
    /// On first call for a symbol, seeds historical data.
    /// </summary>
    public IReadOnlyList<CandlestickEvent> GetHistory(string symbol)
    {
        lock (_lock)
        {
            EnsureSeeded(symbol);

            if (_history.TryGetValue(symbol, out var list))
            {
                return list.ToList(); // snapshot
            }

            return Array.Empty<CandlestickEvent>();
        }
    }

    /// <summary>
    /// Returns all completed candles for all known symbols.
    /// </summary>
    public Dictionary<string, IReadOnlyList<CandlestickEvent>> GetAllHistory()
    {
        lock (_lock)
        {
            // Ensure all known symbols are seeded
            foreach (var symbol in SeedProfiles.Keys)
            {
                EnsureSeeded(symbol);
            }

            var result = new Dictionary<string, IReadOnlyList<CandlestickEvent>>();
            foreach (var (symbol, list) in _history)
            {
                result[symbol] = list.ToList();
            }

            return result;
        }
    }

    // ---- Seed generation (runs once per symbol) ----

    private void EnsureSeeded(string symbol)
    {
        // Must be called inside _lock
        if (_seeded.Contains(symbol)) return;
        _seeded.Add(symbol);

        var profile = SeedProfiles.GetValueOrDefault(symbol)
                      ?? new SymbolSeedProfile(1000m, 0.16);

        var candles = GenerateHistoricalCandles(symbol, profile, _seedCount);

        if (!_history.ContainsKey(symbol))
        {
            _history[symbol] = new List<CandlestickEvent>();
        }

        // Prepend historical candles (they're older than any live data)
        _history[symbol].InsertRange(0, candles);

        // Trim
        while (_history[symbol].Count > _maxCandles)
        {
            _history[symbol].RemoveAt(0);
        }
    }

    /// <summary>
    /// Generates <paramref name="count"/> historical 1-minute OHLCV candles
    /// for a symbol using geometric Brownian motion with mean reversion.
    /// Candles are timestamped backwards from "now" so they appear as recent history.
    /// </summary>
    private static List<CandlestickEvent> GenerateHistoricalCandles(
        string symbol,
        SymbolSeedProfile profile,
        int count)
    {
        var rng = new Random(symbol.GetHashCode()); // deterministic per symbol
        var candles = new List<CandlestickEvent>(count);
        var now = DateTime.UtcNow;

        // Start `count` minutes ago
        var price = profile.BasePrice;
        var annualVol = profile.AnnualVol;

        // Per-tick vol: ~200 ticks per minute at 300ms ticks
        const double ticksPerMinute = 200.0;
        const double ticksPerYear = ticksPerMinute * 60 * 7.5 * 252; // ≈ same as RealisticPriceEngine
        var tickVol = annualVol / Math.Sqrt(ticksPerYear);

        for (var i = count; i >= 1; i--)
        {
            var intervalStart = TruncateToMinute(now.AddMinutes(-i));

            // Simulate ~200 ticks within this 1-minute candle
            var open = price;
            var high = price;
            var low = price;
            long totalVolume = 0;

            const int ticksInCandle = 200;
            for (var t = 0; t < ticksInCandle; t++)
            {
                // GBM step with mean reversion
                var z = NormalSample(rng);
                var deviation = (double)(price - profile.BasePrice) / (double)profile.BasePrice;
                var meanRevPull = -0.0015 * deviation;
                var drift = (rng.NextDouble() - 0.5) * 0.0002; // micro-trend
                var logReturn = meanRevPull + drift + tickVol * z;
                price = Math.Max(100m, Math.Round(price * (decimal)Math.Exp(logReturn), 2));

                high = Math.Max(high, price);
                low = Math.Min(low, price);

                var vol = (long)(5000 * (0.5 + rng.NextDouble()) * (1 + Math.Abs(z) * 0.5));
                totalVolume += vol;
            }

            candles.Add(new CandlestickEvent
            {
                Symbol = symbol,
                IntervalStart = intervalStart,
                Open = open,
                High = high,
                Low = low,
                Close = price,
                Volume = totalVolume,
                TickCount = ticksInCandle,
                IsComplete = true,
            });
        }

        return candles;
    }

    private static DateTime TruncateToMinute(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
    }

    private static double NormalSample(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private record SymbolSeedProfile(decimal BasePrice, double AnnualVol);
}
