using Broker.Service.Services.OptionsRisk;
using Microsoft.AspNetCore.Mvc;
using OptionsRisk.Core.Simulation;

namespace Broker.Service.Controllers;

/// <summary>On-demand educational Monte Carlo for the active isolated session.</summary>
[ApiController]
[Route("api/options/sessions/{id:guid}/monte-carlo")]
[ServiceFilter(typeof(OptionsFeatureGate))]
public sealed class OptionsMonteCarloController : ControllerBase
{
    private readonly OptionsSessionRegistry _sessions;
    public OptionsMonteCarloController(OptionsSessionRegistry sessions) => _sessions = sessions;

    public sealed record Request(int SignedFuturesHedgeLots = 0, int Paths = 5000,
        int Steps = 30, int Seed = 42, double HorizonTradingMinutes = 30);

    [HttpPost]
    public IActionResult Project(Guid id, [FromBody] Request request)
    {
        var session = _sessions.Get(id);
        if (session is null) return NotFound();
        var run = session.Value.Engine.GetRunForProjection();
        if (run is null) return Conflict(new { message = "Start a run first." });
        try
        {
            return Ok(MonteCarloProjector.Compare(run, request.SignedFuturesHedgeLots,
                new MonteCarloSettings(Paths: request.Paths, Steps: request.Steps,
                    Seed: request.Seed, HorizonTradingMinutes: request.HorizonTradingMinutes)));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
