using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Application.Kds.Commands.UpdateKdsStatus;
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
    [HttpGet("tickets")]
    public async Task<ActionResult<List<KdsTicketDto>>> GetTickets(
        [FromQuery] KdsStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tickets = await _mediator.Send(
            new GetKdsTicketsQuery { StatusFilter = status },
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
}
