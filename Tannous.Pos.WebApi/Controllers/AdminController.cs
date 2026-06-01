using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Tannous.Pos.Application.Admin.Commands.PurgeSoftDeletedRecords;
using Tannous.Pos.Application.Admin.Commands.ReconcileReceipts;
using Tannous.Pos.Application.Admin.Commands.ReprintReceipt;
using Tannous.Pos.Application.Admin.Commands.VacuumAnalyze;
using Tannous.Pos.Application.Admin.Queries.GetAdminDatabaseStats;
using Tannous.Pos.Application.DTOs.Printing;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageUsers)]
[EnableRateLimiting("MutationsPerDevice")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("receipts/reconcile")]
    public async Task<IActionResult> ReconcileReceipts()
    {
        var result = await _mediator.Send(new ReconcileReceiptsCommand());
        return Ok(result);
    }

    [HttpPost("orders/{id}/reprint")]
    public async Task<ActionResult<RenderResultDto>> ReprintReceipt(Guid id)
    {
        var result = await _mediator.Send(new ReprintReceiptCommand { OrderId = id });

        if (!result.Found)
            return NotFound();

        if (!result.HasReceiptNumber)
            return BadRequest("Order does not have a receipt number assigned");

        return Ok(result.Receipt);
    }

    [HttpGet("db/stats")]
    public async Task<IActionResult> GetDatabaseStats()
    {
        var result = await _mediator.Send(new GetAdminDatabaseStatsQuery());
        return Ok(result);
    }

    [HttpPost("db/vacuum-analyze")]
    public async Task<IActionResult> VacuumAnalyze()
    {
        var result = await _mediator.Send(new VacuumAnalyzeCommand());
        return Ok(result);
    }

    [HttpPost("purge")]
    public async Task<IActionResult> PurgeSoftDeletedRecords([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new PurgeSoftDeletedRecordsCommand { Days = days });
        return Ok(result);
    }
}
