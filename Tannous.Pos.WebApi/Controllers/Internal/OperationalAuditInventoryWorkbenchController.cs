using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalInventoryWorkbench;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing inventory drift workbench — GET-only, Admin only, aggregate read model.
// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/inventory-workbench")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditInventoryWorkbenchController : ControllerBase
{
    private readonly IOperationalInventoryWorkbenchService _workbench;
    private readonly ILogger<OperationalAuditInventoryWorkbenchController> _logger;

    public OperationalAuditInventoryWorkbenchController(
        IOperationalInventoryWorkbenchService workbench,
        ILogger<OperationalAuditInventoryWorkbenchController> logger)
    {
        _workbench = workbench;
        _logger = logger;
    }

    [HttpGet("drift")]
    public async Task<ActionResult<OperationalInventoryWorkbenchDto>> GetDriftWorkbench(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational inventory workbench observability: drift workbench authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _workbench.GetDriftWorkbenchAsync(cancellationToken));
    }
}
