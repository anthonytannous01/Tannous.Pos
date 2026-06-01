using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalEntityStatus;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Entity operational status — GET-only, Admin only.
// Returns counts and health classification. NOT a record list.
// For record-level detail: GET /internal/operational-audit/timeline/order/{id}
//                          GET /internal/operational-audit/timeline/device/{id}
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/entity-status")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditEntityStatusController : ControllerBase
{
    private readonly IOperationalEntityStatusService _entityStatusService;
    private readonly ILogger<OperationalAuditEntityStatusController> _logger;

    public OperationalAuditEntityStatusController(
        IOperationalEntityStatusService entityStatusService,
        ILogger<OperationalAuditEntityStatusController> logger)
    {
        _entityStatusService = entityStatusService;
        _logger = logger;
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<OperationalOrderStatusDto>> GetOrderStatus(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("OrderStatus", orderId.ToString());
        return Ok(await _entityStatusService.GetOrderStatusAsync(orderId, cancellationToken));
    }

    [HttpGet("device/{deviceId}")]
    public async Task<ActionResult<OperationalDeviceStatusDto>> GetDeviceStatus(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("DeviceStatus", deviceId);
        return Ok(await _entityStatusService.GetDeviceStatusAsync(deviceId, cancellationToken));
    }

    private void LogAuthorization(string scope, string entityId)
    {
        _logger.LogInformation(
            "Operational entity status observability: status authorization path. Policy={Policy}, Scope={Scope}, EntityId={EntityId}, User={User}",
            "Admin",
            scope,
            entityId,
            User.Identity?.Name ?? "unknown");
    }
}
