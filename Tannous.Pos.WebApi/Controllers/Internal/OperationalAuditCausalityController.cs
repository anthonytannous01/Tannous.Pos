using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalCausality;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing causality interpretation — GET-only, Admin only, advisory root-cause explanation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/causality")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditCausalityController : ControllerBase
{
    private readonly IOperationalCausalityService _causalityService;
    private readonly ILogger<OperationalAuditCausalityController> _logger;

    public OperationalAuditCausalityController(
        IOperationalCausalityService causalityService,
        ILogger<OperationalAuditCausalityController> logger)
    {
        _causalityService = causalityService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalCausalChainsDto>> GetCausalChains(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational causality observability: chains authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _causalityService.GetCausalChainsAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalCausalitySummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational causality observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _causalityService.GetCausalitySummaryAsync(cancellationToken));
    }

    [HttpGet("propagation")]
    public async Task<ActionResult<OperationalPropagationAnalysisDto>> GetPropagation(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational causality observability: propagation authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _causalityService.GetPropagationAnalysisAsync(cancellationToken));
    }
}
