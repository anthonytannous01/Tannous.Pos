using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Application.Tables.Commands.CreateTable;
using Tannous.Pos.Application.Tables.Commands.UpdateTableStatus;
using Tannous.Pos.Application.Tables.Queries.GetFloorPlans;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/tables")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class TablesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TablesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns all active floor plans with their tables and live status.</summary>
    [HttpGet("floor-plans")]
    public async Task<ActionResult<List<FloorPlanDto>>> GetFloorPlans(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFloorPlansQuery(), ct));

    /// <summary>Create a new table in a floor plan.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<TableDto>> CreateTable(
        [FromBody] CreateTableDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTableCommand { Table = dto }, ct);
            return CreatedAtAction(nameof(GetFloorPlans), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update the status of a table (Available, Occupied, Reserved, Cleaning).</summary>
    [HttpPatch("{tableId:guid}/status")]
    public async Task<ActionResult<TableDto>> UpdateStatus(
        Guid tableId, [FromBody] UpdateTableStatusDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateTableStatusCommand { TableId = tableId, NewStatus = dto.Status }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
