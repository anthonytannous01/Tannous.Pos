using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational alert signal diagnostics — GET-only, Admin only, query-time heuristics.
// GOVERNANCE / NON-GOAL: not persisted; not delivered externally; no paging/on-call; no automatic remediation.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/alerts")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAlertDiagnosticsController : ControllerBase
{
    private readonly IOperationalAlertSignalService _alertSignals;
    private readonly ILogger<OperationalAlertDiagnosticsController> _logger;

    public OperationalAlertDiagnosticsController(
        IOperationalAlertSignalService alertSignals,
        ILogger<OperationalAlertDiagnosticsController> logger)
    {
        _alertSignals = alertSignals;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalAlertSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _alertSignals.GetAlertSummaryAsync(cancellationToken));
    }

    [HttpGet("current")]
    public async Task<ActionResult<IReadOnlyList<OperationalAlertSignalDto>>> GetCurrent(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Current");
        return Ok(await _alertSignals.GetCurrentSignalsAsync(cancellationToken));
    }

    [HttpGet("critical")]
    public async Task<ActionResult<IReadOnlyList<OperationalAlertSignalDto>>> GetCritical(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Critical");
        return Ok(await _alertSignals.GetCriticalSignalsAsync(cancellationToken));
    }

    [HttpGet("replay-pressure")]
    public async Task<ActionResult<IReadOnlyList<OperationalAlertSignalDto>>> GetReplayPressure(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ReplayPressure");
        return Ok(await _alertSignals.GetReplayPressureSignalsAsync(cancellationToken));
    }

    [HttpGet("inventory-risk")]
    public async Task<ActionResult<IReadOnlyList<OperationalAlertSignalDto>>> GetInventoryRisk(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("InventoryRisk");
        return Ok(await _alertSignals.GetInventoryRiskSignalsAsync(cancellationToken));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational alert observability: alert diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
