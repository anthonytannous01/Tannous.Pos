using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalHandoff;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational handoff continuity — GET-only, Admin only.
// Bounded-window snapshot history projection + current briefing. No recomputation triggered.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/handoff")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditHandoffController : ControllerBase
{
    private readonly IOperationalHandoffService _handoffService;
    private readonly ILogger<OperationalAuditHandoffController> _logger;

    public OperationalAuditHandoffController(
        IOperationalHandoffService handoffService,
        ILogger<OperationalAuditHandoffController> logger)
    {
        _handoffService = handoffService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalHandoffContinuityDto>> GetHandoffContinuity(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Continuity");
        return Ok(await _handoffService.GetHandoffContinuityAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalHandoffSummaryDto>> GetHandoffSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _handoffService.GetHandoffSummaryAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational handoff observability: handoff authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
