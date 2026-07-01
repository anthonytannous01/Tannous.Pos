using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Application.Tables.Commands.CreateFloorPlan;
using Tannous.Pos.Application.Tables.Commands.CreateTable;
using Tannous.Pos.Application.Tables.Commands.UpdateTableStatus;
using Tannous.Pos.Application.Tables.Queries.GetFloorPlans;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/tables")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class TablesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly DbContext _dbContext;

    public TablesController(IMediator mediator, DbContext dbContext)
    {
        _mediator  = mediator;
        _dbContext = dbContext;
    }

    /// <summary>Returns all active floor plans with their tables and live status.</summary>
    [HttpGet("floor-plans")]
    public async Task<ActionResult<List<FloorPlanDto>>> GetFloorPlans(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFloorPlansQuery(), ct));

    /// <summary>Create a new floor plan (zone), e.g. "Indoor", "Terrace".</summary>
    [HttpPost("floor-plans")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<FloorPlanDto>> CreateFloorPlan(
        [FromBody] CreateFloorPlanDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateFloorPlanCommand { FloorPlan = dto }, ct);
        return CreatedAtAction(nameof(GetFloorPlans), result);
    }

    /// <summary>Create a new table inside an existing floor plan.</summary>
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

    /// <summary>Soft-delete a table (sets IsActive = false).</summary>
    [HttpDelete("{tableId:guid}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<IActionResult> DeleteTable(Guid tableId, CancellationToken ct)
    {
        var table = await _dbContext.Set<Table>()
            .FirstOrDefaultAsync(t => t.Id == tableId, ct);

        if (table is null)
            return NotFound();

        table.IsActive = false;
        await _dbContext.SaveChangesAsync(ct);
        return NoContent();
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
