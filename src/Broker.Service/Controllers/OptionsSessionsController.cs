using Broker.Service.Services.OptionsRisk;
using Microsoft.AspNetCore.Mvc;
using static Broker.Service.Services.SseFormatter;

namespace Broker.Service.Controllers;

/// <summary>Isolated per-session educational options API. Legacy /api/options stays available for migration.</summary>
[ApiController]
[Route("api/options/sessions")]
[ServiceFilter(typeof(OptionsFeatureGate))]
public sealed class OptionsSessionsController : ControllerBase
{
    private readonly OptionsSessionRegistry _sessions;
    public OptionsSessionsController(OptionsSessionRegistry sessions) => _sessions = sessions;

    public sealed record CreateRequest(int? Seed);

    [HttpPost]
    public IActionResult Create([FromBody] CreateRequest? request) {
        try {
            var session = _sessions.Create(request?.Seed);
            return session is null
                ? StatusCode(429, new { message = "Session capacity reached." })
                : CreatedAtAction(nameof(State), new { id = session.Value.Id },
                    new { sessionId = session.Value.Id, snapshot = session.Value.Engine.CurrentDto() });
        } catch (InvalidOperationException ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult State(Guid id) {
        var session = _sessions.Get(id);
        return session is null ? NotFound() : Ok(session.Value.Engine.CurrentDto());
    }

    [HttpPost("{id:guid}/pause")]
    public IActionResult Pause(Guid id) => Execute(id, e => e.Pause());

    [HttpPost("{id:guid}/resume")]
    public IActionResult Resume(Guid id) => Execute(id, e => e.Resume());

    public sealed record SpeedRequest(int Speed);

    [HttpPost("{id:guid}/speed")]
    public IActionResult Speed(Guid id, [FromBody] SpeedRequest request) =>
        Execute(id, e => e.SetSpeed(request.Speed));

    [HttpPost("{id:guid}/suggestions/{suggestionId}/apply")]
    public IActionResult Apply(Guid id, string suggestionId) =>
        Execute(id, e => e.ApplyAction(new OptionsRunEngine.ApplyActionRequest(suggestionId)));

    [HttpPost("{id:guid}/suggestions/{suggestionId}/ignore")]
    public IActionResult Ignore(Guid id, string suggestionId) =>
        Execute(id, e => e.IgnoreAction(suggestionId));

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => _sessions.Remove(id) ? NoContent() : NotFound();

    private IActionResult Execute(Guid id, Func<OptionsRunEngine, OptionsSnapshotDto?> action) {
        var session = _sessions.Get(id);
        if (session is null) return NotFound();
        try {
            var dto = action(session.Value.Engine);
            if (dto is null) return NotFound();
            session.Value.Stream.Broadcast(dto);
            return Ok(dto);
        } catch (InvalidOperationException ex) {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/stream")]
    public async Task Stream(Guid id, CancellationToken cancellationToken) {
        var session = _sessions.Get(id);
        if (session is null) { Response.StatusCode = 404; return; }
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");
        var (clientId, channel) = session.Value.Stream.Register();
        try {
            var snapshot = session.Value.Engine.CurrentDto();
            if (snapshot != null) {
                await Response.WriteAsync(FormatSseEvent("snapshot", snapshot), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken)) {
                await Response.WriteAsync(FormatSseEvent("snapshot", item), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        } catch (OperationCanceledException) {
            // Normal client disconnect.
        } finally {
            session.Value.Stream.Unregister(clientId);
        }
    }
}
