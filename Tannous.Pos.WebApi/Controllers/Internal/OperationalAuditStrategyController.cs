using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational strategic posture coordination — GET-only, Admin only.
// Informational strategic stance; no business intelligence, planning, or automation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/strategy")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditStrategyController : ControllerBase
{
    private readonly IOperationalStrategyService _strategyService;
    private readonly ILogger<OperationalAuditStrategyController> _logger;

    public OperationalAuditStrategyController(
        IOperationalStrategyService strategyService,
        ILogger<OperationalAuditStrategyController> logger)
    {
        _strategyService = strategyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalStrategyReportDto>> GetStrategyReport(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Report");
        return Ok(await _strategyService.GetStrategyReportAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalStrategySummaryDto>> GetStrategySummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _strategyService.GetStrategySummaryAsync(cancellationToken));
    }

    [HttpGet("coordination")]
    public async Task<ActionResult<IReadOnlyList<OperationalCoordinationDto>>> GetOperationalCoordination(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Coordination");
        return Ok(await _strategyService.GetOperationalCoordinationAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational strategy observability: strategy authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
