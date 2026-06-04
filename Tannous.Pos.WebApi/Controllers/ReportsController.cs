using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Application.Reports.Queries.GetCogsReport;
using Tannous.Pos.Application.Reports.Queries.GetEodReport;
using Tannous.Pos.Application.Reports.Queries.GetMenuEngineering;
using Tannous.Pos.Application.Reports.Queries.GetSalesSummary;
using MediatR;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyConstants.CanViewReports)]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("eod")]
    public async Task<ActionResult<EodReportDto>> GetEodReport([FromQuery] DateTime? date = null)
    {
        var result = await _mediator.Send(new GetEodReportQuery { Date = date });
        return Ok(result);
    }

    [HttpGet("cogs")]
    public async Task<ActionResult<CogsReportDto>> GetCogsReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var query = new GetCogsReportQuery { From = from, To = to };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Real-time sales summary for the owner dashboard.
    /// Defaults to today (UTC midnight → now). Accepts optional ?from= and ?to= for custom ranges.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<SalesSummaryDto>> GetSalesSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _mediator.Send(new GetSalesSummaryQuery { From = from, To = to });
        return Ok(result);
    }

    /// <summary>
    /// Menu engineering matrix — classifies items as Stars, Plowhorses, Puzzles, Dogs
    /// based on sales popularity vs contribution margin. Requires ?from= and ?to= date range.
    /// </summary>
    [HttpGet("menu-engineering")]
    public async Task<ActionResult<MenuEngineeringReportDto>> GetMenuEngineering(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetMenuEngineeringQuery { From = from, To = to });
        return Ok(result);
    }

    [HttpGet("export/eod.csv")]
    public async Task<IActionResult> ExportEodCsv([FromQuery] DateTime? date = null)
    {
        var report = await _mediator.Send(new GetEodReportQuery { Date = date });

        var csvContent = $"Date,{report.Date:yyyy-MM-dd}\n";
        csvContent += $"Net Sales,{report.NetSales:C}\n";
        csvContent += $"Orders Count,{report.OrdersCount}\n";
        csvContent += $"Average Ticket,{report.AvgTicket:C}\n";
        csvContent += $"Cash Drops,{report.CashDrops:C}\n";
        csvContent += $"Variance,{report.Variance:C}\n\n";
        csvContent += "Top Items\n";
        csvContent += "Item Name,Quantity,Sales\n";

        foreach (var item in report.TopItems)
        {
            csvContent += $"{item.Name},{item.Qty},{item.Sales:C}\n";
        }

        var fileName = $"eod_report_{report.Date:yyyyMMdd}.csv";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", fileName);
    }
}
