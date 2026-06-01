using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalExperienceGraph;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing experience graph — GET-only, Admin only, advisory contextual navigation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/experience-graph")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditExperienceGraphController : ControllerBase
{
    private readonly IOperationalExperienceGraphService _experienceGraphService;
    private readonly ILogger<OperationalAuditExperienceGraphController> _logger;

    public OperationalAuditExperienceGraphController(
        IOperationalExperienceGraphService experienceGraphService,
        ILogger<OperationalAuditExperienceGraphController> logger)
    {
        _experienceGraphService = experienceGraphService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalExperienceGraphDto>> GetExperienceGraph(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational experience graph observability: graph authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _experienceGraphService.GetExperienceGraphAsync(cancellationToken));
    }

    [HttpGet("traversal")]
    public async Task<ActionResult<OperationalExperienceTraversalPathsDto>> GetTraversalPaths(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational experience graph observability: traversal authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _experienceGraphService.GetTraversalPathsAsync(cancellationToken));
    }

    [HttpGet("navigation")]
    public async Task<ActionResult<OperationalContextualNavigationDto>> GetContextualNavigation(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational experience graph observability: navigation authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _experienceGraphService.GetContextualNavigationAsync(cancellationToken));
    }
}
