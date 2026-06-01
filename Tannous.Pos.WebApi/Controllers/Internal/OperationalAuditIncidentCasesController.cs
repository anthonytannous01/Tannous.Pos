using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalIncidents;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing incident cases — GET-only, Admin only, advisory investigation continuity.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/incident-cases")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditIncidentCasesController : ControllerBase
{
    private readonly IOperationalIncidentService _incidentService;
    private readonly ILogger<OperationalAuditIncidentCasesController> _logger;

    public OperationalAuditIncidentCasesController(
        IOperationalIncidentService incidentService,
        ILogger<OperationalAuditIncidentCasesController> logger)
    {
        _incidentService = incidentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalIncidentCasesDto>> GetIncidents(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational incident observability: cases authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _incidentService.GetIncidentCasesAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalIncidentCasesSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational incident observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _incidentService.GetIncidentSummaryAsync(cancellationToken));
    }

    [HttpGet("{incidentId}")]
    public async Task<ActionResult<OperationalIncidentCaseDetailDto>> GetDetails(
        string incidentId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational incident observability: details authorization path. Policy={Policy}, User={User}, IncidentId={IncidentId}",
            "Admin",
            User.Identity?.Name ?? "unknown",
            incidentId);

        var details = await _incidentService.GetIncidentDetailsAsync(incidentId, cancellationToken);
        if (details is null)
            return NotFound();

        return Ok(details);
    }
}
