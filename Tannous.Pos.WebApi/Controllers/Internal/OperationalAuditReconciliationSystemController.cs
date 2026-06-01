using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalReconciliation;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: System-level reconciliation health view — Admin only, GET-only, advisory.
// Reports aggregate unresolved conflict metrics; does not mutate conflict state and performs no auto-healing.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/operational-audit")]
[Authorize(Policy = "Admin")]
public class OperationalAuditReconciliationSystemController : ControllerBase
{
    private readonly IOperationalReconciliationSystemService _reconciliationSystemService;

    public OperationalAuditReconciliationSystemController(
        IOperationalReconciliationSystemService reconciliationSystemService)
    {
        _reconciliationSystemService = reconciliationSystemService;
    }

    /// <summary>
    /// Returns a system-level, non-paginated view of the reconciliation subsystem health.
    /// Reports total unresolved conflict count, oldest conflict age, and entity-type breakdown.
    /// For paginated conflict record detail use GET /conflicts/recent.
    /// Advisory only. GET-only.
    /// </summary>
    [HttpGet("reconciliation/system")]
    public async Task<ActionResult<OperationalReconciliationSystemDto>> GetReconciliationSystem(
        CancellationToken cancellationToken)
    {
        var result = await _reconciliationSystemService
            .GetReconciliationSystemAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }
}
