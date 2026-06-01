using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalResilience;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational resilience diagnostics and survivability cognition — GET-only, Admin only.
// Informational visibility; no throttling, no request shedding, no distributed orchestration.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/resilience")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditResilienceController : ControllerBase
{
    private readonly IOperationalResilienceDiagnosticsService _resilienceService;
    private readonly IOperationalResilienceCognitionService _resilienceCognitionService;
    private readonly ILogger<OperationalAuditResilienceController> _logger;

    public OperationalAuditResilienceController(
        IOperationalResilienceDiagnosticsService resilienceService,
        IOperationalResilienceCognitionService resilienceCognitionService,
        ILogger<OperationalAuditResilienceController> logger)
    {
        _resilienceService = resilienceService;
        _resilienceCognitionService = resilienceCognitionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalResilienceReportDto>> GetResilienceReport(
        CancellationToken cancellationToken = default)
    {
        LogCognitionAuthorization("Report");
        return Ok(await _resilienceCognitionService.GetResilienceReportAsync(cancellationToken));
    }

    [HttpGet("posture/summary")]
    public async Task<ActionResult<OperationalResiliencePostureSummaryDto>> GetPostureSummary(
        CancellationToken cancellationToken = default)
    {
        LogCognitionAuthorization("PostureSummary");
        return Ok(await _resilienceCognitionService.GetResilienceSummaryAsync(cancellationToken));
    }

    [HttpGet("fragility")]
    public async Task<ActionResult<IReadOnlyList<OperationalFragilityDto>>> GetOperationalFragility(
        CancellationToken cancellationToken = default)
    {
        LogCognitionAuthorization("Fragility");
        return Ok(await _resilienceCognitionService.GetOperationalFragilityAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalResilienceSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _resilienceService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("degraded-modes")]
    public async Task<ActionResult<OperationalDegradedModesDto>> GetDegradedModes(CancellationToken cancellationToken = default)
    {
        LogAuthorization("DegradedModes");
        return Ok(await _resilienceService.GetDegradedModesAsync(cancellationToken));
    }

    [HttpGet("pressure-indicators")]
    public async Task<ActionResult<OperationalPressureIndicatorsDto>> GetPressureIndicators(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("PressureIndicators");
        return Ok(await _resilienceService.GetPressureIndicatorsAsync(cancellationToken));
    }

    [HttpGet("replay-risk-summary")]
    public async Task<ActionResult<OperationalReplayRiskSummaryDto>> GetReplayRiskSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ReplayRiskSummary");
        return Ok(await _resilienceService.GetReplayRiskSummaryAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational resilience observability: resilience authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }

    private void LogCognitionAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational resilience observability: cognition authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
