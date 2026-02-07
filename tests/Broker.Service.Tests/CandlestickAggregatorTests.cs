using Broker.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using System.Threading.Channels;

namespace Broker.Service.Tests;

public class CandlestickAggregatorTests
{
    private readonly Channel<CandlestickEvent> _candleChannel;
    private readonly AggregatorTickChannel _aggregatorTickChannel;
    private readonly Mock<ILogger<CandlestickAggregator>> _loggerMock;
    private readonly Mock<ILogger<SseClientManager>> _clientManagerLoggerMock;
    private readonly SseClientManager _clientManager;
    private readonly CandlestickHistory _history;
    private readonly IConfiguration _configuration;

    public CandlestickAggregatorTests()
    {
        _candleChannel = Channel.CreateUnbounded<CandlestickEvent>();
        _aggregatorTickChannel = new AggregatorTickChannel(100);
        _loggerMock = new Mock<ILogger<CandlestickAggregator>>();
        _clientManagerLoggerMock = new Mock<ILogger<SseClientManager>>();

        var configData = new Dictionary<string, string?>
        {
            ["Candlestick:IntervalSeconds"] = "60",
            ["Candlestick:EmitThrottleMs"] = "0", // No throttle for tests
            ["Channel:ClientCapacity"] = "100"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _clientManager = new SseClientManager(_clientManagerLoggerMock.Object, _configuration);
        _history = new CandlestickHistory(60, 0); // No seeding for tests
    }

    private CandlestickAggregator CreateAggregator()
    {
        return new CandlestickAggregator(
            _aggregatorTickChannel,
            _candleChannel,
            _clientManager,
            _history,
            _loggerMock.Object,
            _configuration);
    }

    [Fact]
    public async Task FirstTick_CreatesNewCandle_EmitsInProgressCandle()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var tick = new MarketDataEvent
        {
            Symbol = "NIFTY50",
            Price = 1000m,
            Volume = 5000,
            Timestamp = new DateTime(2026, 1, 15, 10, 30, 15, DateTimeKind.Utc)
        };

        var cts = new CancellationTokenSource();

        // Act — write tick and complete channel so aggregator exits
        await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500); // Allow processing
        cts.Cancel();

        // Assert — should have emitted at least one candle event
        Assert.True(_candleChannel.Reader.TryRead(out var candle));
        Assert.Equal("NIFTY50", candle.Symbol);
        Assert.Equal(1000m, candle.Open);
        Assert.Equal(1000m, candle.High);
        Assert.Equal(1000m, candle.Low);
        Assert.Equal(1000m, candle.Close);
        Assert.Equal(5000, candle.Volume);
        Assert.Equal(1, candle.TickCount);
        Assert.False(candle.IsComplete);
    }

    [Fact]
    public async Task MultipleTicks_SameInterval_UpdatesOHLCV()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var baseTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var ticks = new[]
        {
            new MarketDataEvent { Symbol = "TCS", Price = 100m, Volume = 1000, Timestamp = baseTime.AddSeconds(5) },
            new MarketDataEvent { Symbol = "TCS", Price = 105m, Volume = 2000, Timestamp = baseTime.AddSeconds(10) },
            new MarketDataEvent { Symbol = "TCS", Price = 95m,  Volume = 1500, Timestamp = baseTime.AddSeconds(15) },
            new MarketDataEvent { Symbol = "TCS", Price = 102m, Volume = 3000, Timestamp = baseTime.AddSeconds(20) },
        };

        var cts = new CancellationTokenSource();

        // Act
        foreach (var tick in ticks)
        {
            await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        }
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        // Assert — drain all candle events and check the last one
        CandlestickEvent? lastCandle = null;
        while (_candleChannel.Reader.TryRead(out var candle))
        {
            lastCandle = candle;
        }

        Assert.NotNull(lastCandle);
        Assert.Equal("TCS", lastCandle.Symbol);
        Assert.Equal(100m, lastCandle.Open);   // First tick's price
        Assert.Equal(105m, lastCandle.High);   // Highest price
        Assert.Equal(95m, lastCandle.Low);     // Lowest price
        Assert.Equal(102m, lastCandle.Close);  // Last tick's price
        Assert.Equal(1000 + 2000 + 1500 + 3000, lastCandle.Volume); // Sum of volumes
        Assert.Equal(4, lastCandle.TickCount); // 4 ticks total
    }

    [Fact]
    public async Task IntervalRollover_EmitsCompletedCandle_StartsNewCandle()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var interval1 = new DateTime(2026, 1, 15, 10, 30, 30, DateTimeKind.Utc);
        var interval2 = new DateTime(2026, 1, 15, 10, 31, 10, DateTimeKind.Utc); // Next minute

        var ticks = new[]
        {
            new MarketDataEvent { Symbol = "RELIANCE", Price = 500m, Volume = 1000, Timestamp = interval1 },
            new MarketDataEvent { Symbol = "RELIANCE", Price = 510m, Volume = 2000, Timestamp = interval2 }, // New interval
        };

        var cts = new CancellationTokenSource();

        // Act
        foreach (var tick in ticks)
        {
            await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        }
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        // Assert — collect all emitted candles
        var candles = new List<CandlestickEvent>();
        while (_candleChannel.Reader.TryRead(out var candle))
        {
            candles.Add(candle);
        }

        // Should have: initial incomplete, completed (interval rollover), new incomplete
        Assert.True(candles.Count >= 3, $"Expected at least 3 candle events, got {candles.Count}");

        // Find the completed candle
        var completedCandle = candles.First(c => c.IsComplete);
        Assert.Equal("RELIANCE", completedCandle.Symbol);
        Assert.Equal(500m, completedCandle.Open);
        Assert.Equal(500m, completedCandle.Close);

        // Find the last candle (new interval, in-progress)
        var lastCandle = candles.Last();
        Assert.False(lastCandle.IsComplete);
        Assert.Equal(510m, lastCandle.Open);
    }

    [Fact]
    public async Task CompletedCandle_IsPushedToHistory()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var interval1 = new DateTime(2026, 1, 15, 10, 30, 30, DateTimeKind.Utc);
        var interval2 = new DateTime(2026, 1, 15, 10, 31, 10, DateTimeKind.Utc);

        var ticks = new[]
        {
            new MarketDataEvent { Symbol = "BANKNIFTY", Price = 200m, Volume = 3000, Timestamp = interval1 },
            new MarketDataEvent { Symbol = "BANKNIFTY", Price = 210m, Volume = 4000, Timestamp = interval2 },
        };

        var cts = new CancellationTokenSource();

        // Act
        foreach (var tick in ticks)
        {
            await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        }
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        // Assert — history should contain a completed candle
        var history = _history.GetHistory("BANKNIFTY");
        Assert.Contains(history, c => c.IsComplete && c.Symbol == "BANKNIFTY");
    }

    [Fact]
    public async Task MultipleSymbols_MaintainSeparateCandles()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var baseTime = new DateTime(2026, 1, 15, 10, 30, 10, DateTimeKind.Utc);

        var ticks = new[]
        {
            new MarketDataEvent { Symbol = "NIFTY50", Price = 1000m, Volume = 5000, Timestamp = baseTime },
            new MarketDataEvent { Symbol = "TCS", Price = 300m, Volume = 2000, Timestamp = baseTime.AddSeconds(1) },
            new MarketDataEvent { Symbol = "NIFTY50", Price = 1010m, Volume = 6000, Timestamp = baseTime.AddSeconds(2) },
            new MarketDataEvent { Symbol = "TCS", Price = 305m, Volume = 2500, Timestamp = baseTime.AddSeconds(3) },
        };

        var cts = new CancellationTokenSource();

        foreach (var tick in ticks)
        {
            await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        }
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        // Assert — should have candles for both symbols
        var candles = new List<CandlestickEvent>();
        while (_candleChannel.Reader.TryRead(out var candle))
        {
            candles.Add(candle);
        }

        var niftyCandles = candles.Where(c => c.Symbol == "NIFTY50").ToList();
        var tcsCandles = candles.Where(c => c.Symbol == "TCS").ToList();

        Assert.NotEmpty(niftyCandles);
        Assert.NotEmpty(tcsCandles);

        // NIFTY50: Open should be 1000, High should be 1010
        var lastNifty = niftyCandles.Last();
        Assert.Equal(1000m, lastNifty.Open);
        Assert.Equal(1010m, lastNifty.High);

        // TCS: Open should be 300, High should be 305
        var lastTcs = tcsCandles.Last();
        Assert.Equal(300m, lastTcs.Open);
        Assert.Equal(305m, lastTcs.High);
    }

    [Fact]
    public async Task CandleEvents_BroadcastToSseClients()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var baseTime = new DateTime(2026, 1, 15, 10, 30, 10, DateTimeKind.Utc);
        var nextInterval = new DateTime(2026, 1, 15, 10, 31, 10, DateTimeKind.Utc);

        // Register a client to receive broadcasts
        var (_, clientCandleChannel) = _clientManager.Register("test-client");

        var ticks = new[]
        {
            new MarketDataEvent { Symbol = "NIFTY50", Price = 1000m, Volume = 5000, Timestamp = baseTime },
            new MarketDataEvent { Symbol = "NIFTY50", Price = 1010m, Volume = 6000, Timestamp = nextInterval },
        };

        var cts = new CancellationTokenSource();

        foreach (var tick in ticks)
        {
            await _aggregatorTickChannel.Channel.Writer.WriteAsync(tick);
        }
        _aggregatorTickChannel.Channel.Writer.Complete();

        await aggregator.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        // Assert — client should have received candle events via SseClientManager
        var receivedCandles = new List<CandlestickEvent>();
        while (clientCandleChannel.Reader.TryRead(out var candle))
        {
            receivedCandles.Add(candle);
        }

        Assert.NotEmpty(receivedCandles);
        Assert.Contains(receivedCandles, c => c.Symbol == "NIFTY50");
    }
}
