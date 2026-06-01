using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalSituationRoom;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing situation room briefing — GET-only, Admin only, advisory executive synthesis.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/situation-room")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditSituationRoomController : ControllerBase
{
    private readonly IOperationalSituationRoomService _situationRoomService;
    private readonly ILogger<OperationalAuditSituationRoomController> _logger;

    public OperationalAuditSituationRoomController(
        IOperationalSituationRoomService situationRoomService,
        ILogger<OperationalAuditSituationRoomController> logger)
    {
        _situationRoomService = situationRoomService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalSituationRoomDto>> GetSituationRoom(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational situation room observability: situation room authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _situationRoomService.GetSituationRoomAsync(cancellationToken));
    }

    [HttpGet("briefing")]
    public async Task<ActionResult<OperationalExecutiveBriefingDto>> GetExecutiveBriefing(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational situation room observability: executive briefing authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _situationRoomService.GetExecutiveBriefingAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalSituationSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational situation room observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _situationRoomService.GetSituationSummaryAsync(cancellationToken));
    }
}
