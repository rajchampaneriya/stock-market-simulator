using Broker.Service.Services;
using Shared.Contracts;

namespace Broker.Service.Tests;

public class MarketEventBufferTests
{
    private MarketEventBuffer CreateBuffer(int maxSize = 1000)
    {
        return new MarketEventBuffer(maxSize);
    }

    private MarketDataEvent CreateTick(string symbol = "NIFTY50", decimal price = 1000m)
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
    public void Add_StoresEvent_IncreasesBufferCount()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick());

        Assert.Equal(1, buffer.BufferCount);
    }

    [Fact]
    public void Add_AssignsSequentialIds()
    {
        var buffer = CreateBuffer();

        var event1 = buffer.Add(CreateTick("NIFTY50"));
        var event2 = buffer.Add(CreateTick("TCS"));
        var event3 = buffer.Add(CreateTick("RELIANCE"));

        Assert.Equal("1", event1.Id);
        Assert.Equal("2", event2.Id);
        Assert.Equal("3", event3.Id);
    }

    [Fact]
    public void Add_SetsEventTypeToMarketdata()
    {
        var buffer = CreateBuffer();

        var sseItem = buffer.Add(CreateTick());

        Assert.Equal("marketdata", sseItem.EventType);
    }

    [Fact]
    public void Add_StoresOriginalData()
    {
        var buffer = CreateBuffer();
        var tick = CreateTick("TCS", 500m);

        var sseItem = buffer.Add(tick);

        Assert.Equal("TCS", sseItem.Data.Symbol);
        Assert.Equal(500m, sseItem.Data.Price);
    }

    [Fact]
    public void Add_EvictsOldestWhenMaxSizeReached()
    {
        var buffer = CreateBuffer(maxSize: 3);

        buffer.Add(CreateTick("A", 100m));
        buffer.Add(CreateTick("B", 200m));
        buffer.Add(CreateTick("C", 300m));
        buffer.Add(CreateTick("D", 400m)); // Should evict "A"

        Assert.Equal(3, buffer.BufferCount);

        // GetEventsAfter("0") should return all — "A" (id=1) should be gone
        var events = buffer.GetEventsAfter("0").ToList();
        Assert.Equal(3, events.Count);
        Assert.Equal("2", events[0].Id); // "A" was evicted
        Assert.Equal("B", events[0].Data.Symbol);
    }

    [Fact]
    public void GetEventsAfter_ReturnsEventsAfterGivenId()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick("A"));
        buffer.Add(CreateTick("B"));
        buffer.Add(CreateTick("C"));
        buffer.Add(CreateTick("D"));
        buffer.Add(CreateTick("E"));

        var events = buffer.GetEventsAfter("3").ToList();

        Assert.Equal(2, events.Count);
        Assert.Equal("D", events[0].Data.Symbol);
        Assert.Equal("E", events[1].Data.Symbol);
    }

    [Fact]
    public void GetEventsAfter_ReturnsEmptyWhenNoNewEvents()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick("A"));
        buffer.Add(CreateTick("B"));

        var events = buffer.GetEventsAfter("2").ToList();

        Assert.Empty(events);
    }

    [Fact]
    public void GetEventsAfter_ReturnsAllEventsForInvalidId()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick("A"));
        buffer.Add(CreateTick("B"));
        buffer.Add(CreateTick("C"));

        var events = buffer.GetEventsAfter("not-a-number").ToList();

        Assert.Equal(3, events.Count);
    }

    [Fact]
    public void GetEventsAfter_ReturnsAllEventsForIdZero()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick("A"));
        buffer.Add(CreateTick("B"));

        var events = buffer.GetEventsAfter("0").ToList();

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void CurrentEventId_ReflectsLastAssignedId()
    {
        var buffer = CreateBuffer();

        Assert.Equal(0, buffer.CurrentEventId);

        buffer.Add(CreateTick());
        Assert.Equal(1, buffer.CurrentEventId);

        buffer.Add(CreateTick());
        buffer.Add(CreateTick());
        Assert.Equal(3, buffer.CurrentEventId);
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        var buffer = CreateBuffer();

        buffer.Add(CreateTick("A"));
        buffer.Add(CreateTick("B"));
        buffer.Add(CreateTick("C"));

        buffer.Clear();

        Assert.Equal(0, buffer.BufferCount);
    }

    [Fact]
    public async Task Buffer_IsThreadSafe_ConcurrentAddAndRead()
    {
        var buffer = CreateBuffer(maxSize: 500);
        var exceptions = new List<Exception>();

        // Writer thread
        var writeTask = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 1000; i++)
                {
                    buffer.Add(CreateTick($"SYM{i}", i));
                }
            }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        // Reader thread
        var readTask = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 1000; i++)
                {
                    _ = buffer.GetEventsAfter("0");
                    _ = buffer.BufferCount;
                    _ = buffer.CurrentEventId;
                }
            }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        await Task.WhenAll(writeTask, readTask);

        Assert.Empty(exceptions);
    }

    [Fact]
    public void GetEventsAfter_WithEvictedId_ReturnsAllRemainingEvents()
    {
        var buffer = CreateBuffer(maxSize: 3);

        buffer.Add(CreateTick("A")); // id=1, will be evicted
        buffer.Add(CreateTick("B")); // id=2, will be evicted
        buffer.Add(CreateTick("C")); // id=3
        buffer.Add(CreateTick("D")); // id=4, evicts A
        buffer.Add(CreateTick("E")); // id=5, evicts B

        // Client asks for events after id=1, which was evicted
        var events = buffer.GetEventsAfter("1").ToList();

        // Should return all events with id > 1 that are still in buffer
        Assert.Equal(3, events.Count);
        Assert.Equal("C", events[0].Data.Symbol);
        Assert.Equal("D", events[1].Data.Symbol);
        Assert.Equal("E", events[2].Data.Symbol);
    }
}
