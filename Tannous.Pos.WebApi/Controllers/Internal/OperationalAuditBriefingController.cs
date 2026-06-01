using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalBriefing;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational briefing — GET-only, Admin only.
// Point-in-time snapshot from existing cognition stores. No recomputation triggered.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/briefing")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditBriefingController : ControllerBase
{
    private readonly IOperationalBriefingService _briefingService;
    private readonly ILogger<OperationalAuditBriefingController> _logger;

    public OperationalAuditBriefingController(
        IOperationalBriefingService briefingService,
        ILogger<OperationalAuditBriefingController> logger)
    {
        _briefingService = briefingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalBriefingPackageDto>> GetBriefingPackage(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Package");
        return Ok(await _briefingService.GetBriefingPackageAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalBriefingSummaryDto>> GetBriefingSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _briefingService.GetBriefingSummaryAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational briefing observability: briefing authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
