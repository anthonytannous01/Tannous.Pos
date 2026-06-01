using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational audit diagnostics surface — read-only, Owner (Admin) policy, no payload disclosure.
// Not for customer/mobile usage. Does not mutate business state. Append-only audit records projected to safe DTOs.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditDiagnosticsController : ControllerBase
{
    private readonly IOperationalAuditQueryService _queryService;
    private readonly ILogger<OperationalAuditDiagnosticsController> _logger;

    public OperationalAuditDiagnosticsController(
        IOperationalAuditQueryService queryService,
        ILogger<OperationalAuditDiagnosticsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet("timeline/order/{orderId:guid}")]
    public async Task<ActionResult<OperationalAuditPageDto>> GetOrderTimeline(
        Guid orderId,
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "OrderTimeline",
            User.Identity?.Name ?? "unknown");

        var filter = BuildFilter(category, action, severity, fromUtc, toUtc);
        var result = await _queryService.GetOrderTimelineAsync(orderId, filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("timeline/device/{deviceId}")]
    public async Task<ActionResult<OperationalAuditPageDto>> GetDeviceTimeline(
        string deviceId,
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "DeviceTimeline",
            User.Identity?.Name ?? "unknown");

        var filter = BuildFilter(category, action, severity, fromUtc, toUtc);
        var result = await _queryService.GetDeviceTimelineAsync(deviceId, filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("timeline/operation/{operationId}")]
    public async Task<ActionResult<OperationalAuditPageDto>> GetOperationTimeline(
        string operationId,
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "OperationTimeline",
            User.Identity?.Name ?? "unknown");

        var filter = BuildFilter(category, action, severity, fromUtc, toUtc);
        var result = await _queryService.GetOperationTimelineAsync(operationId, filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("timeline/entity/{entityType}/{entityId:guid}")]
    public async Task<ActionResult<OperationalAuditPageDto>> GetEntityTimeline(
        string entityType,
        Guid entityId,
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "EntityTimeline",
            User.Identity?.Name ?? "unknown");

        var filter = BuildFilter(category, action, severity, fromUtc, toUtc);
        var result = await _queryService.GetEntityTimelineAsync(entityType, entityId, filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conflicts/recent")]
    public async Task<ActionResult<OperationalAuditPageDto>> GetRecentConflicts(
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OperationalAuditQueryConstants.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            "RecentConflicts",
            User.Identity?.Name ?? "unknown");

        var filter = BuildFilter(category, action, severity, fromUtc, toUtc);
        var result = await _queryService.GetRecentConflictsAsync(filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    private static OperationalAuditQueryFilter BuildFilter(
        string? category,
        string? action,
        string? severity,
        DateTime? fromUtc,
        DateTime? toUtc) =>
        new()
        {
            Category = category,
            Action = action,
            Severity = severity,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
}
