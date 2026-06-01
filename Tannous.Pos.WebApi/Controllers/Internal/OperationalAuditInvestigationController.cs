using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.OperationalInvestigation;

namespace Tannous.Pos.WebApi.Controllers.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/operational-audit")]
[Authorize(Policy = "Admin")]
public class OperationalAuditInvestigationController : ControllerBase
{
    private readonly IOperationalInvestigationService _investigationService;

    public OperationalAuditInvestigationController(IOperationalInvestigationService investigationService)
    {
        _investigationService = investigationService;
    }

    /// <summary>
    /// Returns a correlated investigation view for the specified order.
    /// Combines entity health, top audit highlights (Warning/Critical), and system cognition context.
    /// Advisory only. GET-only.
    /// </summary>
    [HttpGet("investigation/order/{orderId:guid}")]
    public async Task<ActionResult<OperationalOrderInvestigationDto>> GetOrderInvestigation(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _investigationService
            .GetOrderInvestigationAsync(orderId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Returns a correlated investigation view for the specified device.
    /// Combines device health, top audit highlights (Warning/Critical), receipt outcomes, and system cognition context.
    /// Advisory only. GET-only.
    /// </summary>
    [HttpGet("investigation/device/{deviceId}")]
    public async Task<ActionResult<OperationalDeviceInvestigationDto>> GetDeviceInvestigation(
        string deviceId,
        CancellationToken cancellationToken)
    {
        var result = await _investigationService
            .GetDeviceInvestigationAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }
}
