using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Dynamic incident correlation diagnostics — GET-only, Admin only, aggregates only.
// Heuristic causality grouping; no PagerDuty, no OpenTelemetry, no automatic remediation, no persistent incident store.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/incidents")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditIncidentsController : ControllerBase
{
    private readonly IOperationalIncidentCorrelationService _incidentCorrelation;
    private readonly ILogger<OperationalAuditIncidentsController> _logger;

    public OperationalAuditIncidentsController(
        IOperationalIncidentCorrelationService incidentCorrelation,
        ILogger<OperationalAuditIncidentsController> logger)
    {
        _incidentCorrelation = incidentCorrelation;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalIncidentSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _incidentCorrelation.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("high-risk")]
    public async Task<ActionResult<OperationalIncidentPageDto>> GetHighRisk(CancellationToken cancellationToken = default)
    {
        LogAuthorization("HighRisk");
        return Ok(await _incidentCorrelation.GetHighRiskAsync(cancellationToken));
    }

    [HttpGet("by-order/{orderId:guid}")]
    public async Task<ActionResult<OperationalIncidentPageDto>> GetByOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ByOrder");
        return Ok(await _incidentCorrelation.GetByOrderIdAsync(orderId, cancellationToken));
    }

    [HttpGet("by-device/{deviceId}")]
    public async Task<ActionResult<OperationalIncidentPageDto>> GetByDevice(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ByDevice");
        return Ok(await _incidentCorrelation.GetByDeviceIdAsync(deviceId, cancellationToken));
    }

    [HttpGet("by-operation/{operationId}")]
    public async Task<ActionResult<OperationalIncidentPageDto>> GetByOperation(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ByOperation");
        return Ok(await _incidentCorrelation.GetByOperationIdAsync(operationId, cancellationToken));
    }

    [HttpGet("cascading-degradation")]
    public async Task<ActionResult<OperationalCascadingDegradationDto>> GetCascadingDegradation(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("CascadingDegradation");
        return Ok(await _incidentCorrelation.GetCascadingDegradationAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational incident observability: incident authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
