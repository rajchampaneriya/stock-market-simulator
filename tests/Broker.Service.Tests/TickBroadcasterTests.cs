using Broker.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;

namespace Broker.Service.Tests;

public class TickBroadcasterTests
{
    private readonly AggregatorTickChannel _aggregatorChannel;
    private readonly SseClientManager _clientManager;
    private readonly Mock<ILogger<TickBroadcaster>> _loggerMock;

    public TickBroadcasterTests()
    {
        _aggregatorChannel = new AggregatorTickChannel(100);
        _loggerMock = new Mock<ILogger<TickBroadcaster>>();

        var clientLoggerMock = new Mock<ILogger<SseClientManager>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Channel:ClientCapacity"] = "100"
            })
            .Build();

        _clientManager = new SseClientManager(clientLoggerMock.Object, config);
    }

    private TickBroadcaster CreateBroadcaster()
    {
        return new TickBroadcaster(_clientManager, _aggregatorChannel, _loggerMock.Object);
    }

    private static MarketDataEvent CreateTick(string symbol = "NIFTY50", decimal price = 1000m)
    {
        return new MarketDataEvent
        {
            Symbol = symbol,
            Price = price,
            Volume = 5000,
            Timestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public void Broadcast_WritesToAggregatorChannel()
    {
        var broadcaster = CreateBroadcaster();
        var tick = CreateTick();

        broadcaster.Broadcast(tick);

        Assert.True(_aggregatorChannel.Channel.Reader.TryRead(out var received));
        Assert.Equal(tick.Symbol, received.Symbol);
        Assert.Equal(tick.Price, received.Price);
    }

    [Fact]
    public void Broadcast_WritesToAllRegisteredSseClients()
    {
        var broadcaster = CreateBroadcaster();

        // Register two SSE clients
        var (client1Tick, _) = _clientManager.Register("client-1");
        var (client2Tick, _) = _clientManager.Register("client-2");

        var tick = CreateTick("TCS", 500m);

        // Act
        broadcaster.Broadcast(tick);

        // Assert — both clients should receive the tick
        Assert.True(client1Tick.Reader.TryRead(out var received1));
        Assert.Equal("TCS", received1.Symbol);
        Assert.Equal(500m, received1.Price);

        Assert.True(client2Tick.Reader.TryRead(out var received2));
        Assert.Equal("TCS", received2.Symbol);
        Assert.Equal(500m, received2.Price);
    }

    [Fact]
    public void Broadcast_WithNoClients_StillWritesToAggregator()
    {
        var broadcaster = CreateBroadcaster();
        var tick = CreateTick();

        broadcaster.Broadcast(tick);

        Assert.True(_aggregatorChannel.Channel.Reader.TryRead(out var received));
        Assert.Equal(tick.Symbol, received.Symbol);
    }

    [Fact]
    public void Broadcast_ClientChannelFull_DoesNotAffectOtherClients()
    {
        // Use a tiny capacity for client manager
        var clientLoggerMock = new Mock<ILogger<SseClientManager>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Channel:ClientCapacity"] = "2" // Very small
            })
            .Build();
        var manager = new SseClientManager(clientLoggerMock.Object, config);

        var broadcaster = new TickBroadcaster(manager, _aggregatorChannel, _loggerMock.Object);

        var (client1Tick, _) = manager.Register("client-1");
        var (client2Tick, _) = manager.Register("client-2");

        // Fill client-1's channel (capacity=2, DropOldest means writes always succeed)
        broadcaster.Broadcast(CreateTick("A", 100m));
        broadcaster.Broadcast(CreateTick("B", 200m));
        broadcaster.Broadcast(CreateTick("C", 300m)); // Drops oldest for client-1

        // Client-2 should still have all 3 ticks (with oldest dropped)
        var client2Ticks = new List<MarketDataEvent>();
        while (client2Tick.Reader.TryRead(out var t))
            client2Ticks.Add(t);

        Assert.Equal(2, client2Ticks.Count); // Capacity is 2, oldest dropped
        Assert.Equal("B", client2Ticks[0].Symbol);
        Assert.Equal("C", client2Ticks[1].Symbol);
    }

    [Fact]
    public void Broadcast_AfterClientUnregistered_DoesNotDeliverToUnregistered()
    {
        var broadcaster = CreateBroadcaster();

        var (client1Tick, _) = _clientManager.Register("client-1");
        var (client2Tick, _) = _clientManager.Register("client-2");

        // Unregister client-1
        _clientManager.Unregister("client-1");

        var tick = CreateTick("RELIANCE", 800m);
        broadcaster.Broadcast(tick);

        // client-1's channel was completed on unregister
        Assert.False(client1Tick.Reader.TryRead(out _));

        // client-2 should still receive
        Assert.True(client2Tick.Reader.TryRead(out var received));
        Assert.Equal("RELIANCE", received.Symbol);
    }

    [Fact]
    public void Broadcast_MultipleTicks_AllReachAllClients()
    {
        var broadcaster = CreateBroadcaster();

        var (clientTick, _) = _clientManager.Register("client-1");

        var ticks = new[]
        {
            CreateTick("NIFTY50", 1000m),
            CreateTick("TCS", 300m),
            CreateTick("RELIANCE", 500m),
        };

        // Act
        foreach (var tick in ticks)
        {
            broadcaster.Broadcast(tick);
        }

        // Assert
        var received = new List<MarketDataEvent>();
        while (clientTick.Reader.TryRead(out var t))
            received.Add(t);

        Assert.Equal(3, received.Count);
        Assert.Equal("NIFTY50", received[0].Symbol);
        Assert.Equal("TCS", received[1].Symbol);
        Assert.Equal("RELIANCE", received[2].Symbol);
    }

    [Fact]
    public void SseClientManager_Register_IncreasesActiveCount()
    {
        Assert.Equal(0, _clientManager.ActiveClientCount);

        _clientManager.Register("c1");
        Assert.Equal(1, _clientManager.ActiveClientCount);

        _clientManager.Register("c2");
        Assert.Equal(2, _clientManager.ActiveClientCount);
    }

    [Fact]
    public void SseClientManager_Unregister_DecreasesActiveCount()
    {
        _clientManager.Register("c1");
        _clientManager.Register("c2");

        _clientManager.Unregister("c1");
        Assert.Equal(1, _clientManager.ActiveClientCount);

        _clientManager.Unregister("c2");
        Assert.Equal(0, _clientManager.ActiveClientCount);
    }

    [Fact]
    public void SseClientManager_BroadcastCandle_ReachesAllClients()
    {
        var (_, client1Candle) = _clientManager.Register("c1");
        var (_, client2Candle) = _clientManager.Register("c2");

        var candle = new CandlestickEvent
        {
            Symbol = "NIFTY50",
            Open = 1000m,
            High = 1010m,
            Low = 995m,
            Close = 1005m,
            Volume = 50000,
            TickCount = 100,
            IsComplete = true,
            IntervalStart = DateTime.UtcNow
        };

        _clientManager.BroadcastCandle(candle);

        Assert.True(client1Candle.Reader.TryRead(out var c1));
        Assert.Equal("NIFTY50", c1.Symbol);
        Assert.True(c1.IsComplete);

        Assert.True(client2Candle.Reader.TryRead(out var c2));
        Assert.Equal("NIFTY50", c2.Symbol);
    }
}
