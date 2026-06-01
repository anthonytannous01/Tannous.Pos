using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Read-only forensic snapshot export for incident investigation — Admin only, no mutations, no payload disclosure.
// Not legal evidence, not immutable ledger, not customer-visible. Append-only audit semantics preserved.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/export")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditForensicExportController : ControllerBase
{
    private readonly IOperationalForensicSnapshotService _forensicService;
    private readonly ILogger<OperationalAuditForensicExportController> _logger;

    public OperationalAuditForensicExportController(
        IOperationalForensicSnapshotService forensicService,
        ILogger<OperationalAuditForensicExportController> logger)
    {
        _forensicService = forensicService;
        _logger = logger;
    }

    [HttpGet("conflict/{conflictId:guid}")]
    public async Task<ActionResult<OperationalForensicSnapshotDto>> ExportByConflict(
        Guid conflictId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Conflict");
        var snapshot = await _forensicService.ExportByConflictIdAsync(conflictId, cancellationToken);
        if (snapshot == null)
            return NotFound();

        return Ok(snapshot);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<OperationalForensicSnapshotDto>> ExportByOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Order");
        var snapshot = await _forensicService.ExportByOrderIdAsync(orderId, cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet("operation/{operationId}")]
    public async Task<ActionResult<OperationalForensicSnapshotDto>> ExportByOperation(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Operation");
        var snapshot = await _forensicService.ExportByOperationIdAsync(operationId, cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet("device/{deviceId}")]
    public async Task<ActionResult<OperationalForensicSnapshotDto>> ExportByDevice(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorizationPath("Device");
        var snapshot = await _forensicService.ExportByDeviceIdAsync(deviceId, cancellationToken);
        return Ok(snapshot);
    }

    private void LogAuthorizationPath(string scope)
    {
        _logger.LogInformation(
            "Operational forensic observability: forensic authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
