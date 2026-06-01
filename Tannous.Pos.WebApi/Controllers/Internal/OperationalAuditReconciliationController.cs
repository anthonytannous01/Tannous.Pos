using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operator-driven sync conflict reconciliation workflow — Admin only.
// Diagnostics workflow only: no auto-healing, no deletes, append-only status transitions.
// Status changes are recorded as append-only OperationalAuditRecord entries under ReconciliationWorkflow.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/reconciliation")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditReconciliationController : ControllerBase
{
    private readonly ISyncConflictReconciliationService _reconciliationService;
    private readonly ILogger<OperationalAuditReconciliationController> _logger;

    public OperationalAuditReconciliationController(
        ISyncConflictReconciliationService reconciliationService,
        ILogger<OperationalAuditReconciliationController> logger)
    {
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    [HttpGet("unresolved")]
    public async Task<ActionResult<SyncConflictPageDto>> GetUnresolved(
        [FromQuery] string? conflictType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Unresolved");
        var filter = new SyncConflictQueryFilter
        {
            ConflictType = conflictType,
            UnresolvedOnly = true,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        var result = await _reconciliationService.GetUnresolvedAsync(filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<SyncConflictPageDto>> GetByStatus(
        string status,
        [FromQuery] string? conflictType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("ByStatus");
        var filter = new SyncConflictQueryFilter
        {
            ResolutionStatus = status,
            ConflictType = conflictType,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        var result = await _reconciliationService.GetByStatusAsync(status, filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReconciliationSummaryDto>> GetSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Summary");
        var result = await _reconciliationService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("acknowledge/{id:guid}")]
    public async Task<ActionResult<SyncConflictItemDto>> Acknowledge(
        Guid id,
        [FromBody] ReconciliationStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Acknowledge");
        var result = await _reconciliationService.AcknowledgeAsync(id, request ?? new ReconciliationStatusChangeRequest(), Actor(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("investigate/{id:guid}")]
    public async Task<ActionResult<SyncConflictItemDto>> Investigate(
        Guid id,
        [FromBody] ReconciliationStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Investigate");
        var result = await _reconciliationService.InvestigateAsync(id, request ?? new ReconciliationStatusChangeRequest(), Actor(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("resolve/{id:guid}")]
    public async Task<ActionResult<SyncConflictItemDto>> Resolve(
        Guid id,
        [FromBody] ReconciliationStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Resolve");
        var result = await _reconciliationService.ResolveAsync(id, request ?? new ReconciliationStatusChangeRequest(), Actor(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("ignore/{id:guid}")]
    public async Task<ActionResult<SyncConflictItemDto>> Ignore(
        Guid id,
        [FromBody] ReconciliationStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Ignore");
        var result = await _reconciliationService.IgnoreAsync(id, request ?? new ReconciliationStatusChangeRequest(), Actor(), cancellationToken);
        return Ok(result);
    }

    private string Actor() => User.Identity?.Name ?? "unknown";

    private void LogAuthorizationPath(string scope)
    {
        _logger.LogInformation(
            "Operational reconciliation workflow observability: reconciliation authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
