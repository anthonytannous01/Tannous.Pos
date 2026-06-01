using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalNavigation;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing navigation index — GET-only, Admin only, advisory routing guidance.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/navigation")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditNavigationController : ControllerBase
{
    private readonly IOperationalNavigationService _navigationService;
    private readonly ILogger<OperationalAuditNavigationController> _logger;

    public OperationalAuditNavigationController(
        IOperationalNavigationService navigationService,
        ILogger<OperationalAuditNavigationController> logger)
    {
        _navigationService = navigationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalNavigationIndexDto>> GetNavigation(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational navigation observability: index authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _navigationService.GetNavigationIndexAsync(cancellationToken));
    }

    [HttpGet("routes")]
    public async Task<ActionResult<IReadOnlyList<OperationalNavigationRouteDto>>> GetRoutes(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational navigation observability: routes authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _navigationService.GetNavigationRoutesAsync(cancellationToken));
    }
}
