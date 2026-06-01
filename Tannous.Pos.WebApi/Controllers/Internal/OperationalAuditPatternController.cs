using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalPatterns;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing pattern intelligence — GET-only, Admin only, advisory pattern recognition.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/patterns")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditPatternController : ControllerBase
{
    private readonly IOperationalPatternService _patternService;
    private readonly ILogger<OperationalAuditPatternController> _logger;

    public OperationalAuditPatternController(
        IOperationalPatternService patternService,
        ILogger<OperationalAuditPatternController> logger)
    {
        _patternService = patternService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalPatternsDto>> GetOperationalPatterns(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational pattern observability: patterns authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _patternService.GetOperationalPatternsAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalPatternSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational pattern observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _patternService.GetPatternSummaryAsync(cancellationToken));
    }

    [HttpGet("archetypes")]
    public async Task<ActionResult<OperationalStabilizationArchetypesDto>> GetArchetypes(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational pattern observability: archetypes authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _patternService.GetStabilizationArchetypesAsync(cancellationToken));
    }
}
