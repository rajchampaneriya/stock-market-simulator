using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Broker.Service.Services.OptionsRisk;

/// <summary>
/// Per-client bounded channels for the options snapshot SSE stream,
/// mirroring the SseClientManager fan-out pattern used for market ticks.
/// </summary>
public sealed class OptionsStreamManager
{
    private readonly ConcurrentDictionary<string, Channel<OptionsSnapshotDto>> _clients = new();

    public (string ClientId, Channel<OptionsSnapshotDto> Channel) Register()
    {
        var clientId = Guid.NewGuid().ToString("N");
        var channel = Channel.CreateBounded<OptionsSnapshotDto>(new BoundedChannelOptions(64)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropOldest,
        });
        _clients[clientId] = channel;
        return (clientId, channel);
    }

    public void Unregister(string clientId) => _clients.TryRemove(clientId, out _);

    public void Broadcast(OptionsSnapshotDto snapshot)
    {
        foreach (var channel in _clients.Values)
            channel.Writer.TryWrite(snapshot);
    }

    public bool HasClients => !_clients.IsEmpty;
}
