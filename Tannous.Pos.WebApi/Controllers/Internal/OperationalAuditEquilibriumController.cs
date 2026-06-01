using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalEquilibrium;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational equilibrium cognition — GET-only, Admin only.
// Informational balance interpretation; not control theory, optimization, or simulation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/equilibrium")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditEquilibriumController : ControllerBase
{
    private readonly IOperationalEquilibriumService _equilibriumService;
    private readonly ILogger<OperationalAuditEquilibriumController> _logger;

    public OperationalAuditEquilibriumController(
        IOperationalEquilibriumService equilibriumService,
        ILogger<OperationalAuditEquilibriumController> logger)
    {
        _equilibriumService = equilibriumService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalEquilibriumReportDto>> GetEquilibriumReport(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Report");
        return Ok(await _equilibriumService.GetEquilibriumReportAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalEquilibriumSummaryDto>> GetEquilibriumSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _equilibriumService.GetEquilibriumSummaryAsync(cancellationToken));
    }

    [HttpGet("imbalances")]
    public async Task<ActionResult<IReadOnlyList<OperationalImbalanceDto>>> GetOperationalImbalances(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Imbalances");
        return Ok(await _equilibriumService.GetOperationalImbalancesAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational equilibrium observability: equilibrium authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
