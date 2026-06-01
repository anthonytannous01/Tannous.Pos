using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalDashboard;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operator-facing operational dashboard — GET-only, Admin only, aggregate read model.
// GOVERNANCE / NON-GOAL: not deployment gating; not authoritative business truth; no payload/timeline exposure.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/dashboard")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditDashboardController : ControllerBase
{
    private readonly IOperationalDashboardService _dashboard;
    private readonly ILogger<OperationalAuditDashboardController> _logger;

    public OperationalAuditDashboardController(
        IOperationalDashboardService dashboard,
        ILogger<OperationalAuditDashboardController> logger)
    {
        _dashboard = dashboard;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalDashboardSummaryDto>> GetSummary(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational dashboard observability: dashboard authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _dashboard.GetSummaryAsync(cancellationToken));
    }
}
