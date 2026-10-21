using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Broker.Service.Services.OptionsRisk;

/// <summary>
/// In-memory isolated simulation hosts. Every session has its own price generator,
/// spike seed, strategy state, lock and stream. Expired sessions are disposed by the host.
/// This is single-process state: sticky routing or external persistence is required for scale-out.
/// </summary>
public sealed class OptionsSessionRegistry : IDisposable
{
    private sealed record Entry(OptionsRunEngine Engine, OptionsStreamManager Stream, DateTime CreatedUtc);
    private readonly ConcurrentDictionary<Guid, Entry> _sessions = new();
    private readonly OptionsRiskSettings _settings;
    private readonly ILoggerFactory _logs;
    private readonly TimeSpan _ttl = TimeSpan.FromHours(2);
    private const int MaxSessions = 100;

    public OptionsSessionRegistry(OptionsRiskSettings settings, ILoggerFactory logs) {
        _settings = settings;
        _logs = logs;
    }

    public (Guid Id, OptionsRunEngine Engine, OptionsStreamManager Stream)? Create(int? seed) {
        Prune();
        if (_sessions.Count >= MaxSessions) return null;
        var stream = new OptionsStreamManager();
        var engine = new OptionsRunEngine(_settings, stream, _logs.CreateLogger<OptionsRunEngine>());
        var id = Guid.NewGuid();
        try {
            engine.Start(seed);
            if (!_sessions.TryAdd(id, new Entry(engine, stream, DateTime.UtcNow))) {
                engine.Dispose();
                return null;
            }
            _ = engine.StartAsync(CancellationToken.None);
            return (id, engine, stream);
        } catch {
            engine.Dispose();
            throw;
        }
    }

    public (OptionsRunEngine Engine, OptionsStreamManager Stream)? Get(Guid id) {
        Prune();
        return _sessions.TryGetValue(id, out var entry) ? (entry.Engine, entry.Stream) : null;
    }

    public bool Remove(Guid id) {
        if (!_sessions.TryRemove(id, out var entry)) return false;
        entry.Engine.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        entry.Engine.Dispose();
        return true;
    }

    public void Prune() {
        foreach (var pair in _sessions)
            if (DateTime.UtcNow - pair.Value.CreatedUtc > _ttl)
                Remove(pair.Key);
    }

    public void Dispose() {
        foreach (var id in _sessions.Keys) Remove(id);
    }
}
