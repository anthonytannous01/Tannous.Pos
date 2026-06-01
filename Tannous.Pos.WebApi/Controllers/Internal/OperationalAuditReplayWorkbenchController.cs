using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalReplayWorkbench;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing replay pressure workbench — GET-only, Admin only, aggregate read model.
// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/replay-workbench")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditReplayWorkbenchController : ControllerBase
{
    private readonly IOperationalReplayWorkbenchService _workbench;
    private readonly ILogger<OperationalAuditReplayWorkbenchController> _logger;

    public OperationalAuditReplayWorkbenchController(
        IOperationalReplayWorkbenchService workbench,
        ILogger<OperationalAuditReplayWorkbenchController> logger)
    {
        _workbench = workbench;
        _logger = logger;
    }

    [HttpGet("pressure")]
    public async Task<ActionResult<OperationalReplayWorkbenchDto>> GetPressureWorkbench(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational replay workbench observability: pressure workbench authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _workbench.GetPressureWorkbenchAsync(cancellationToken));
    }
}
