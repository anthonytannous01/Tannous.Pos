using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing reconciliation workbench — GET-only, Admin only, aggregate read model.
// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/workbench")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditWorkbenchController : ControllerBase
{
    private readonly IOperationalReconciliationWorkbenchService _workbench;
    private readonly ILogger<OperationalAuditWorkbenchController> _logger;

    public OperationalAuditWorkbenchController(
        IOperationalReconciliationWorkbenchService workbench,
        ILogger<OperationalAuditWorkbenchController> logger)
    {
        _workbench = workbench;
        _logger = logger;
    }

    [HttpGet("reconciliation")]
    public async Task<ActionResult<OperationalReconciliationWorkbenchDto>> GetReconciliationWorkbench(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational workbench observability: reconciliation workbench authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _workbench.GetReconciliationWorkbenchAsync(cancellationToken));
    }
}
