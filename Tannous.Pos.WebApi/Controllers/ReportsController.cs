using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Application.Reports.Queries.GetCogsReport;
using Tannous.Pos.Application.Reports.Queries.GetDemandForecast;
using Tannous.Pos.Application.Reports.Queries.GetEodReport;
using Tannous.Pos.Application.Reports.Queries.GetMenuEngineering;
using Tannous.Pos.Application.Reports.Queries.GetSalesSummary;
using Tannous.Pos.Application.Reports.Queries.GetSalesExport;
using Tannous.Pos.Application.Reports.Queries.GetPurchasesExport;
using MediatR;
using Tannous.Pos.WebApi.Constants;
using System.Text;

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
        [FromQuery] DateTime? from     = null,
        [FromQuery] DateTime? to       = null,
        [FromQuery] Guid?     branchId = null)
    {
        var result = await _mediator.Send(new GetSalesSummaryQuery { From = from, To = to, BranchId = branchId });
        return Ok(result);
    }

    /// <summary>
    /// Rule-based demand forecast for a target date (default: tomorrow).
    /// Uses same-day-of-week rolling average over the past 4 weeks.
    /// Returns estimated order count, revenue, time block breakdown, top items, and ingredient demand.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<DemandForecastDto>> GetDemandForecast(
        [FromQuery] DateTime? targetDate = null,
        [FromQuery] Guid?     branchId   = null)
    {
        var result = await _mediator.Send(new GetDemandForecastQuery { TargetDate = targetDate, BranchId = branchId });
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

    /// <summary>
    /// Full sales export — one row per paid order, suitable for loading into Excel or accounting software.
    /// Includes order number, receipt, type, subtotal, tax, stamp duty, total, payment methods.
    /// </summary>
    [HttpGet("export/sales.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportSalesCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid?    branchId = null)
    {
        var rows = await _mediator.Send(new GetSalesExportQuery { From = from, To = to, BranchId = branchId });

        var sb = new StringBuilder();
        sb.AppendLine("Date,OrderNumber,ReceiptNumber,OrderType,CustomerName,SubTotal,TaxAmount,StampDuty,Total,Discount,ChangeDue,PaymentMethods,Currencies,BranchId");

        foreach (var r in rows)
        {
            sb.AppendLine(
                $"{r.Date:yyyy-MM-dd HH:mm}," +
                $"{CsvEscape(r.OrderNumber)}," +
                $"{CsvEscape(r.ReceiptNumber)}," +
                $"{r.OrderType}," +
                $"{CsvEscape(r.CustomerName)}," +
                $"{r.SubTotal:F2}," +
                $"{r.TaxAmount:F2}," +
                $"{r.StampDuty:F2}," +
                $"{r.Total:F2}," +
                $"{r.Discount:F2}," +
                $"{r.ChangeDue:F2}," +
                $"{CsvEscape(r.PaymentMethods)}," +
                $"{CsvEscape(r.Currencies)}," +
                $"{r.BranchId}");
        }

        var fileName = $"sales_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    /// <summary>
    /// Purchase orders export — for supplier reconciliation and accounts payable.
    /// </summary>
    [HttpGet("export/purchases.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportPurchasesCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var rows = await _mediator.Send(new GetPurchasesExportQuery { From = from, To = to });

        var sb = new StringBuilder();
        sb.AppendLine("Date,OrderNumber,Supplier,Status,SubTotal,TaxAmount,Total,Lines,Notes");

        foreach (var r in rows)
        {
            sb.AppendLine(
                $"{r.Date:yyyy-MM-dd}," +
                $"{CsvEscape(r.OrderNumber)}," +
                $"{CsvEscape(r.Supplier)}," +
                $"{r.Status}," +
                $"{r.SubTotal:F2}," +
                $"{r.TaxAmount:F2}," +
                $"{r.Total:F2}," +
                $"{r.LineCount}," +
                $"{CsvEscape(r.Notes)}");
        }

        var fileName = $"purchases_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
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
