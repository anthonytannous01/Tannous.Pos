using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Application.Reservations.Commands.CreateReservation;
using Tannous.Pos.Application.Reservations.Commands.UpdateReservationStatus;
using Tannous.Pos.Application.Reservations.Queries.GetAvailableTables;
using Tannous.Pos.Application.Reservations.Queries.GetReservations;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reservations")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>List reservations — filter by date range, branch, status.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations(
        [FromQuery] Guid?              branchId = null,
        [FromQuery] DateTime?          from     = null,
        [FromQuery] DateTime?          to       = null,
        [FromQuery] ReservationStatus? status   = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetReservationsQuery
        {
            BranchId = branchId, From = from, To = to, Status = status
        }, ct));

    /// <summary>Available tables for a given slot and party size.</summary>
    [HttpGet("available-tables")]
    public async Task<ActionResult<IEnumerable<AvailableTableDto>>> GetAvailableTables(
        [FromQuery] DateTime slot,
        [FromQuery] int      partySize = 1,
        [FromQuery] Guid?    branchId  = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAvailableTablesQuery
        {
            SlotDateTime = slot, PartySize = partySize, BranchId = branchId
        }, ct));

    /// <summary>Create a new reservation.</summary>
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(
        [FromBody] CreateReservationCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetReservations), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update reservation status (Confirm / Seat / Cancel / NoShow).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ReservationDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateReservationStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateReservationStatusCommand
            {
                ReservationId = id,
                NewStatus     = request.Status,
                TableId       = request.TableId
            }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public class UpdateReservationStatusRequest
{
    public ReservationStatus Status  { get; set; }
    public Guid?             TableId { get; set; }
}
