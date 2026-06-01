using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalAttention;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational attention coordination — GET-only, Admin only.
// Informational priority routing; no workflow orchestration, alerting, or automation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/attention")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditAttentionController : ControllerBase
{
    private readonly IOperationalAttentionService _attentionService;
    private readonly ILogger<OperationalAuditAttentionController> _logger;

    public OperationalAuditAttentionController(
        IOperationalAttentionService attentionService,
        ILogger<OperationalAuditAttentionController> logger)
    {
        _attentionService = attentionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalAttentionReportDto>> GetAttentionReport(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Report");
        return Ok(await _attentionService.GetAttentionReportAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalAttentionSummaryDto>> GetAttentionSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _attentionService.GetAttentionSummaryAsync(cancellationToken));
    }

    [HttpGet("priorities")]
    public async Task<ActionResult<IReadOnlyList<OperationalPriorityDto>>> GetOperationalPriorities(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Priorities");
        return Ok(await _attentionService.GetOperationalPrioritiesAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational attention observability: attention authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
