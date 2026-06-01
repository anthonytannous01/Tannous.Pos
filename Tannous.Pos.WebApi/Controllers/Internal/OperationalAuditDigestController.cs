using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalDigest;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing operational digest — GET-only, Admin only, advisory condensation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/digest")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditDigestController : ControllerBase
{
    private readonly IOperationalDigestService _digestService;
    private readonly ILogger<OperationalAuditDigestController> _logger;

    public OperationalAuditDigestController(
        IOperationalDigestService digestService,
        ILogger<OperationalAuditDigestController> logger)
    {
        _digestService = digestService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalDigestDto>> GetOperationalDigest(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational digest observability: digest authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _digestService.GetOperationalDigestAsync(cancellationToken));
    }

    [HttpGet("executive")]
    public async Task<ActionResult<OperationalExecutiveDigestDto>> GetExecutiveDigest(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational digest observability: executive authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _digestService.GetExecutiveDigestAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalDigestSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational digest observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _digestService.GetDigestSummaryAsync(cancellationToken));
    }
}
