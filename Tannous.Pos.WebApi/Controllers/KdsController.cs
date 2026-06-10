using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Application.Kds.Commands.AssignMenuItemsToStation;
using Tannous.Pos.Application.Kds.Commands.CreateKdsStation;
using Tannous.Pos.Application.Kds.Commands.DeleteKdsStation;
using Tannous.Pos.Application.Kds.Commands.UpdateKdsStatus;
using Tannous.Pos.Application.Kds.Commands.UpdateKdsStation;
using Tannous.Pos.Application.Kds.Queries.GetKdsStations;
using Tannous.Pos.Application.Kds.Queries.GetKdsTickets;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

/// <summary>
/// Kitchen Display System endpoints.
/// Kitchen screens poll GET /kds/tickets to receive active order lines.
/// Kitchen staff PATCH individual lines to update their preparation status.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/kds")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanViewKds)]
public class KdsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KdsController> _logger;

    public KdsController(IMediator mediator, ILogger<KdsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Returns all active KDS tickets (Pending + InProgress by default).
    /// Kitchen screens should poll this endpoint on a short interval (e.g. every 5 seconds).
    /// </summary>
    /// <param name="status">Optional filter: 0=Pending, 1=InProgress, 2=Done, 3=Cancelled</param>
    /// <param name="stationId">Optional filter: only tickets for menu items assigned to this station.</param>
    [HttpGet("tickets")]
    public async Task<ActionResult<List<KdsTicketDto>>> GetTickets(
        [FromQuery] KdsStatus? status = null,
        [FromQuery] Guid? stationId = null,
        CancellationToken cancellationToken = default)
    {
        var tickets = await _mediator.Send(
            new GetKdsTicketsQuery { StatusFilter = status, StationFilter = stationId },
            cancellationToken);
        return Ok(tickets);
    }

    /// <summary>
    /// Updates the KDS status of a single order line.
    /// Transitions: Pending → InProgress → Done.
    /// </summary>
    [HttpPatch("tickets/{orderLineId:guid}/status")]
    public async Task<ActionResult<KdsTicketDto>> UpdateStatus(
        Guid orderLineId,
        [FromBody] UpdateKdsStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateKdsStatusCommand
                {
                    OrderLineId = orderLineId,
                    NewStatus   = dto.Status
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("KDS status update rejected. OrderLineId={Id}, Reason={Reason}",
                orderLineId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Returns active kitchen stations, optionally filtered by branch.</summary>
    [HttpGet("stations")]
    public async Task<ActionResult<List<KdsStationDto>>> GetStations(
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var stations = await _mediator.Send(
            new GetKdsStationsQuery { BranchId = branchId },
            cancellationToken);
        return Ok(stations);
    }

    [HttpPost("stations")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<KdsStationDto>> CreateStation(
        [FromBody] CreateKdsStationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CreateKdsStationCommand
            {
                Name         = request.Name,
                NameAr       = request.NameAr,
                Color        = request.Color,
                DisplayOrder = request.DisplayOrder,
                BranchId     = request.BranchId
            },
            cancellationToken);
        return CreatedAtAction(nameof(GetStations), new { id = result.Id }, result);
    }

    [HttpPut("stations/{id:guid}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<KdsStationDto>> UpdateStation(
        Guid id,
        [FromBody] UpdateKdsStationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateKdsStationCommand
            {
                Id           = id,
                Name         = request.Name,
                NameAr       = request.NameAr,
                Color        = request.Color,
                DisplayOrder = request.DisplayOrder,
                IsActive     = request.IsActive
            },
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("stations/{id:guid}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult> DeleteStation(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteKdsStationCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("stations/assign")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<int>> AssignMenuItems(
        [FromBody] AssignMenuItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = await _mediator.Send(
            new AssignMenuItemsToStationCommand
            {
                StationId   = request.StationId,
                MenuItemIds = request.MenuItemIds
            },
            cancellationToken);
        return Ok(count);
    }
}

public record CreateKdsStationRequest(string Name, string? NameAr, string? Color, int DisplayOrder, Guid? BranchId);
public record UpdateKdsStationRequest(string Name, string? NameAr, string? Color, int DisplayOrder, bool IsActive);
public record AssignMenuItemsRequest(Guid? StationId, List<Guid> MenuItemIds);
