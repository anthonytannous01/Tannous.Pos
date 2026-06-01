using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing recovery posture — GET-only, Admin only, advisory stabilization outlook.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/recovery")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditRecoveryController : ControllerBase
{
    private readonly IOperationalRecoveryService _recoveryService;
    private readonly ILogger<OperationalAuditRecoveryController> _logger;

    public OperationalAuditRecoveryController(
        IOperationalRecoveryService recoveryService,
        ILogger<OperationalAuditRecoveryController> logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalRecoveryPostureDto>> GetRecovery(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational recovery observability: posture authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _recoveryService.GetRecoveryPostureAsync(cancellationToken));
    }

    [HttpGet("outlook")]
    public async Task<ActionResult<OperationalRecoveryOutlookDto>> GetOutlook(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational recovery observability: outlook authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _recoveryService.GetRecoveryOutlookAsync(cancellationToken));
    }
}
