using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing short-window trend read models — GET-only, Admin only, advisory comparison.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/trends")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditTrendController : ControllerBase
{
    private readonly IOperationalTrendService _trendService;
    private readonly ILogger<OperationalAuditTrendController> _logger;

    public OperationalAuditTrendController(
        IOperationalTrendService trendService,
        ILogger<OperationalAuditTrendController> logger)
    {
        _trendService = trendService;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalTrendSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational trend observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _trendService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("deltas")]
    public async Task<ActionResult<IReadOnlyList<OperationalTrendDeltaDto>>> GetDeltas(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational trend observability: deltas authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _trendService.GetDeltasAsync(cancellationToken));
    }
}
