using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalSimulation;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing hypothetical simulation — GET-only, Admin only, advisory what-if analysis.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/simulation")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditSimulationController : ControllerBase
{
    private readonly IOperationalSimulationService _simulationService;
    private readonly ILogger<OperationalAuditSimulationController> _logger;

    public OperationalAuditSimulationController(
        IOperationalSimulationService simulationService,
        ILogger<OperationalAuditSimulationController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalSimulationScenariosDto>> GetSimulationScenarios(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational simulation observability: scenarios authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _simulationService.GetSimulationScenariosAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalSimulationSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational simulation observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _simulationService.GetSimulationSummaryAsync(cancellationToken));
    }

    [HttpGet("outlook")]
    public async Task<ActionResult<OperationalSimulationOutlookDto>> GetOutlook(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational simulation observability: outlook authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _simulationService.GetSimulationOutlookAsync(cancellationToken));
    }
}
