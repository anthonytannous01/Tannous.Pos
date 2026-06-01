using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing topology intelligence — GET-only, Admin only, advisory dependency mapping.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/topology")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditTopologyController : ControllerBase
{
    private readonly IOperationalTopologyService _topologyService;
    private readonly ILogger<OperationalAuditTopologyController> _logger;

    public OperationalAuditTopologyController(
        IOperationalTopologyService topologyService,
        ILogger<OperationalAuditTopologyController> logger)
    {
        _topologyService = topologyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalTopologyDto>> GetOperationalTopology(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational topology observability: topology authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _topologyService.GetOperationalTopologyAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalTopologySummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational topology observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _topologyService.GetTopologySummaryAsync(cancellationToken));
    }

    [HttpGet("chains")]
    public async Task<ActionResult<IReadOnlyList<OperationalDependencyChainDto>>> GetDependencyChains(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational topology observability: dependency chains authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _topologyService.GetDependencyChainsAsync(cancellationToken));
    }
}
