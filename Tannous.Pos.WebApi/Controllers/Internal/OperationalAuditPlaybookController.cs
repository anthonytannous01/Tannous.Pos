using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalPlaybooks;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// INTERNAL: Operator-facing response playbooks — GET-only, Admin only, advisory stabilization guidance.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/playbooks")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditPlaybookController : ControllerBase
{
    private readonly IOperationalPlaybookService _playbookService;
    private readonly ILogger<OperationalAuditPlaybookController> _logger;

    public OperationalAuditPlaybookController(
        IOperationalPlaybookService playbookService,
        ILogger<OperationalAuditPlaybookController> logger)
    {
        _playbookService = playbookService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<OperationalPlaybooksDto>> GetOperationalPlaybooks(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational playbook observability: playbooks authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _playbookService.GetOperationalPlaybooksAsync(cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalPlaybookSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational playbook observability: summary authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _playbookService.GetPlaybookSummaryAsync(cancellationToken));
    }

    [HttpGet("stabilization-guidance")]
    public async Task<ActionResult<OperationalStabilizationGuidanceDto>> GetStabilizationGuidance(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Operational playbook observability: stabilization guidance authorization path. Policy={Policy}, User={User}",
            "Admin",
            User.Identity?.Name ?? "unknown");

        return Ok(await _playbookService.GetStabilizationGuidanceAsync(cancellationToken));
    }
}
