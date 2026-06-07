using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Delivery.Commands.CreateDeliveryInfo;
using Tannous.Pos.Application.Delivery.Commands.UpdateDeliveryStatus;
using Tannous.Pos.Application.Delivery.Queries.GetDeliveryQueue;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/delivery")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class DeliveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeliveryController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Active delivery queue — Pending/Assigned/PickedUp/OnWay by default.
    /// Pass ?status= to filter to a specific status or view completed deliveries.
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<DeliveryDto>>> GetQueue(
        [FromQuery] Guid?           branchId = null,
        [FromQuery] DeliveryStatus? status   = null,
        [FromQuery] DateTime?       from     = null,
        [FromQuery] DateTime?       to       = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDeliveryQueueQuery
        {
            BranchId = branchId, Status = status, From = from, To = to
        }, ct));

    /// <summary>
    /// Attach delivery info to an existing Delivery-type order.
    /// Call this immediately after creating a delivery order to register address and channel.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DeliveryDto>> Create(
        [FromBody] CreateDeliveryInfoCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetQueue), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update delivery status and optionally assign/update driver.
    /// Transitions: Pending → Assigned → PickedUp/OnWay → Delivered | Failed | Cancelled
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<DeliveryDto>> UpdateStatus(
        Guid id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateDeliveryStatusCommand
            {
                DeliveryId  = id,
                NewStatus   = request.Status,
                DriverName  = request.DriverName,
                DriverPhone = request.DriverPhone
            }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public class UpdateDeliveryStatusRequest
{
    public DeliveryStatus Status      { get; set; }
    public string?        DriverName  { get; set; }
    public string?        DriverPhone { get; set; }
}
