using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalTriage;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing triage queue — GET-only, Admin only, advisory investigation prioritization.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/triage")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditTriageController : ControllerBase
{
    private readonly IOperationalTriageService _triageService;
    private readonly ILogger<OperationalAuditTriageController> _logger;

    public OperationalAuditTriageController(
        IOperationalTriageService triageService,
        ILogger<OperationalAuditTriageController> logger)
    {
        _triageService = triageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalTriageQueueDto>> GetTriage(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational triage observability: queue authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _triageService.GetTriageQueueAsync(cancellationToken));
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<IReadOnlyList<OperationalTriageRecommendationDto>>> GetRecommendations(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational triage observability: recommendations authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _triageService.GetRecommendationsAsync(cancellationToken));
    }
}
