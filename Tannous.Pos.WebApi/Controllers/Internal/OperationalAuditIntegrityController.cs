using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalIntegrity;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing interpretation integrity — GET-only, Admin only, advisory consistency verification.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/integrity")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditIntegrityController : ControllerBase
{
    private readonly IOperationalIntegrityService _integrityService;
    private readonly ILogger<OperationalAuditIntegrityController> _logger;

    public OperationalAuditIntegrityController(
        IOperationalIntegrityService integrityService,
        ILogger<OperationalAuditIntegrityController> logger)
    {
        _integrityService = integrityService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalIntegrityReportDto>> GetIntegrityReport(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational integrity observability: report authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _integrityService.GetIntegrityReportAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalIntegritySummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational integrity observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _integrityService.GetIntegritySummaryAsync(cancellationToken));
    }

    [HttpGet("contradictions")]
    public async Task<ActionResult<OperationalIntegrityContradictionsDto>> GetContradictions(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational integrity observability: contradictions authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _integrityService.GetContradictionsAsync(cancellationToken));
    }
}
