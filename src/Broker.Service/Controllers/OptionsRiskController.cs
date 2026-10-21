using Broker.Service.Services;
using Broker.Service.Services.OptionsRisk;
using Microsoft.AspNetCore.Mvc;
using static Broker.Service.Services.SseFormatter;

namespace Broker.Service.Controllers;

/// <summary>
/// Educational options selling risk simulator: control a run (start/pause/speed,
/// apply or ignore coach suggestions) and stream snapshots over SSE.
/// Educational only — no real orders, no broker integration.
/// </summary>
[ApiController]
[Route("api/options")]
[ServiceFilter(typeof(OptionsFeatureGate))]
public sealed class OptionsRiskController : ControllerBase
{
    private readonly OptionsRunEngine _engine;
    private readonly OptionsStreamManager _stream;
    private readonly OptionsRiskSettings _settings;
    private readonly ILogger<OptionsRiskController> _logger;

    public OptionsRiskController(
        OptionsRunEngine engine,
        OptionsStreamManager stream,
        OptionsRiskSettings settings,
        ILogger<OptionsRiskController> logger)
    {
        _engine = engine;
        _stream = stream;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Feature flag + tuned parameters for the UI.</summary>
    [HttpGet("config")]
    public ActionResult<OptionsConfigDto> GetConfig() => Ok(new OptionsConfigDto(
        Enabled: true,
        LotSize: _settings.LotSize,
        StrikeStep: _settings.StrikeStep,
        WingOffset: _settings.WingOffset,
        Capital: _settings.Capital,
        DaysToExpiry: _settings.DaysToExpiry,
        Speeds: _settings.EffectiveSpeeds,
        BrokerProfile: _settings.BrokerProfile));

    [HttpGet("state")]
    public ActionResult<OptionsSnapshotDto> GetState()
    {
        var dto = _engine.CurrentDto();
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    public sealed record StartRequest(int? Seed);

    [HttpPost("start")]
    public IActionResult Start([FromBody] StartRequest? request) =>
        Ok(_engine.Start(request?.Seed));

    [HttpPost("pause")]
    public IActionResult Pause()
    {
        var dto = _engine.Pause();
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    [HttpPost("resume")]
    public IActionResult Resume()
    {
        var dto = _engine.Resume();
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    public sealed record SpeedRequest(int Speed);

    [HttpPost("speed")]
    public IActionResult SetSpeed([FromBody] SpeedRequest request)
    {
        var dto = _engine.SetSpeed(request.Speed);
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    [HttpPost("suggestions/{suggestionId}/apply")]
    public IActionResult Apply(string suggestionId)
    {
        var dto = _engine.ApplyAction(new OptionsRunEngine.ApplyActionRequest(suggestionId));
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    [HttpPost("suggestions/{suggestionId}/ignore")]
    public IActionResult Ignore(string suggestionId)
    {
        var dto = _engine.IgnoreAction(suggestionId);
        return dto == null ? NotFound(new { message = "No run started." }) : Ok(dto);
    }

    /// <summary>SSE stream of full snapshots (one event per simulated tick).</summary>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var (clientId, channel) = _stream.Register();
        _logger.LogInformation("Options SSE client {ClientId} connected", clientId);

        try
        {
            var existing = _engine.CurrentDto();
            if (existing != null)
            {
                await Response.WriteAsync(FormatSseEvent("snapshot", existing), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            else
            {
                await Response.WriteAsync(FormatSseEvent("connected", new { clientId }), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            var lastHeartbeat = DateTime.UtcNow;
            await foreach (var snapshot in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await Response.WriteAsync(FormatSseEvent("snapshot", snapshot), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= 15)
                {
                    await Response.WriteAsync(FormatSseEvent("heartbeat", new { timestamp = DateTime.UtcNow }), cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    lastHeartbeat = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Options SSE client {ClientId} disconnected", clientId);
        }
        finally
        {
            _stream.Unregister(clientId);
        }
    }
}
