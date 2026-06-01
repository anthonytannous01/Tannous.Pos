using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalTimeline;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing operational timeline — GET-only, Admin only, advisory chronology.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/timeline")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditTimelineController : ControllerBase
{
    private readonly IOperationalTimelineService _timelineService;
    private readonly ILogger<OperationalAuditTimelineController> _logger;

    public OperationalAuditTimelineController(
        IOperationalTimelineService timelineService,
        ILogger<OperationalAuditTimelineController> logger)
    {
        _timelineService = timelineService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalTimelineDto>> GetTimeline(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational timeline observability: timeline authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _timelineService.GetTimelineAsync(cancellationToken));
    }

    [HttpGet("correlations")]
    public async Task<ActionResult<IReadOnlyList<OperationalTimelineCorrelationDto>>> GetCorrelations(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational timeline observability: correlations authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _timelineService.GetCorrelationsAsync(cancellationToken));
    }
}
