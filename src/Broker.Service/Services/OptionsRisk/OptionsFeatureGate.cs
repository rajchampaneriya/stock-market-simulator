using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Broker.Service.Services.OptionsRisk;

/// <summary>
/// Returns 404 for every options endpoint when Features:EnableOptionsRisk is off,
/// so the feature stays hidden without touching the existing pipeline.
/// </summary>
public sealed class OptionsFeatureGate(IConfiguration configuration) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!configuration.GetValue<bool>("Features:EnableOptionsRisk"))
            context.Result = new NotFoundObjectResult(new { message = "Feature disabled." });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
