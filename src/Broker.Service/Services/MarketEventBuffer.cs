using Shared.Contracts;

namespace Broker.Service.Services;

/// <summary>
/// Represents a Server-Sent Event item with metadata
/// </summary>
public class SseEventItem<T>
{
    public T Data { get; set; } = default!;
    public string? Id { get; set; }
    public string? EventType { get; set; }
}

/// <summary>
/// Buffers SSE events to support event replay on reconnection using Last-Event-ID
/// </summary>
public class MarketEventBuffer
{
    private readonly LinkedList<SseEventItem<MarketDataEvent>> _buffer = new();
    private readonly int _maxBufferSize;
    private readonly object _lock = new();
    private long _eventCounter = 0;

    public MarketEventBuffer(int maxBufferSize = 1000)
    {
        _maxBufferSize = maxBufferSize;
    }

    /// <summary>
    /// Adds a market data event to the buffer and returns it wrapped as an SSE item
    /// </summary>
    public SseEventItem<MarketDataEvent> Add(MarketDataEvent marketData)
    {
        lock (_lock)
        {
            var eventId = Interlocked.Increment(ref _eventCounter);
            var sseItem = new SseEventItem<MarketDataEvent>
            {
                Data = marketData,
                EventType = "marketdata",
                Id = eventId.ToString()
            };

            _buffer.AddLast(sseItem);

            // Remove oldest events if buffer is full
            if (_buffer.Count > _maxBufferSize)
            {
                _buffer.RemoveFirst();
            }

            return sseItem;
        }
    }

    /// <summary>
    /// Gets all events that occurred after the specified event ID
    /// </summary>
    public IEnumerable<SseEventItem<MarketDataEvent>> GetEventsAfter(string lastEventId)
    {
        lock (_lock)
        {
            if (!long.TryParse(lastEventId, out var lastId))
            {
                // If the last event ID is invalid, return all buffered events
                return _buffer.ToList();
            }

            // Find all events with ID greater than lastEventId
            return _buffer
                .Where(item => long.TryParse(item.Id, out var id) && id > lastId)
                .ToList();
        }
    }

    /// <summary>
    /// Gets the current event count
    /// </summary>
    public long CurrentEventId => Interlocked.Read(ref _eventCounter);

    /// <summary>
    /// Gets the number of events currently in the buffer
    /// </summary>
    public int BufferCount
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Count;
            }
        }
    }

    /// <summary>
    /// Clears all buffered events
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }
}
