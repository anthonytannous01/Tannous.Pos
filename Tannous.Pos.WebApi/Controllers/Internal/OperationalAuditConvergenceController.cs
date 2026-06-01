using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalConvergence;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing convergence intelligence — GET-only, Admin only, advisory signal convergence.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/convergence")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditConvergenceController : ControllerBase
{
    private readonly IOperationalConvergenceService _convergenceService;
    private readonly ILogger<OperationalAuditConvergenceController> _logger;

    public OperationalAuditConvergenceController(
        IOperationalConvergenceService convergenceService,
        ILogger<OperationalAuditConvergenceController> logger)
    {
        _convergenceService = convergenceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalConvergenceReportDto>> GetConvergenceReport(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational convergence observability: report authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _convergenceService.GetConvergenceReportAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalConvergenceSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational convergence observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _convergenceService.GetConvergenceSummaryAsync(cancellationToken));
    }

    [HttpGet("divergence")]
    public async Task<ActionResult<IReadOnlyList<OperationalDivergenceDto>>> GetOperationalDivergence(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational convergence observability: divergence authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _convergenceService.GetOperationalDivergenceAsync(cancellationToken));
    }
}
