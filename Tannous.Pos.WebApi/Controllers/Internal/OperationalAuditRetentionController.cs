using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational retention lifecycle summary — read-only, Admin only, no deletion/archival workers.
// Not legal evidence, not immutable compliance archive, not customer-visible.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/retention")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditRetentionController : ControllerBase
{
    private readonly IOperationalRetentionSummaryService _retentionSummaryService;
    private readonly ILogger<OperationalAuditRetentionController> _logger;

    public OperationalAuditRetentionController(
        IOperationalRetentionSummaryService retentionSummaryService,
        ILogger<OperationalAuditRetentionController> logger)
    {
        _retentionSummaryService = retentionSummaryService;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalRetentionSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational retention observability: retention authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "Summary",
            User.Identity?.Name ?? "unknown");

        var summary = await _retentionSummaryService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
