using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalEvolution;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing evolution timeline — GET-only, Admin only, advisory transition intelligence.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/evolution")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditEvolutionController : ControllerBase
{
    private readonly IOperationalEvolutionService _evolutionService;
    private readonly ILogger<OperationalAuditEvolutionController> _logger;

    public OperationalAuditEvolutionController(
        IOperationalEvolutionService evolutionService,
        ILogger<OperationalAuditEvolutionController> logger)
    {
        _evolutionService = evolutionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalEvolutionTimelineDto>> GetEvolutionTimeline(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational evolution observability: timeline authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _evolutionService.GetEvolutionTimelineAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalEvolutionSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational evolution observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _evolutionService.GetEvolutionSummaryAsync(cancellationToken));
    }

    [HttpGet("momentum")]
    public async Task<ActionResult<OperationalMomentumAnalysisDto>> GetMomentumAnalysis(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational evolution observability: momentum authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _evolutionService.GetMomentumAnalysisAsync(cancellationToken));
    }
}
