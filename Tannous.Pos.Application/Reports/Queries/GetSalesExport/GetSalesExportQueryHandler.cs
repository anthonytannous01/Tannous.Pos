using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reports.Queries.GetSalesExport;

public class GetSalesExportQueryHandler
    : IRequestHandler<GetSalesExportQuery, IEnumerable<SalesExportRowDto>>
{
    private readonly DbContext _db;

    public GetSalesExportQueryHandler(DbContext db) => _db = db;

    public async Task<IEnumerable<SalesExportRowDto>> Handle(
        GetSalesExportQuery request, CancellationToken ct)
    {
        var query = _db.Set<Order>()
            .Include(o => o.Payments)
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid
                     && o.CreatedAt >= request.From
                     && o.CreatedAt <= request.To);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        var orders = await query.OrderBy(o => o.CreatedAt).ToListAsync(ct);

        return orders.Select(o => new SalesExportRowDto
        {
            Date           = o.ClosedAt ?? o.CreatedAt,
            OrderNumber    = o.OrderNumber,
            ReceiptNumber  = o.ReceiptNumber,
            OrderType      = o.OrderType.ToString(),
            CustomerName   = o.CustomerName,
            BranchId       = o.BranchId?.ToString(),
            SubTotal       = o.SubTotal,
            TaxAmount      = o.TaxAmount,
            StampDuty      = o.StampDutyAmount,
            Total          = o.TotalAmount,
            Discount       = o.DiscountAmount,
            ChangeDue      = o.ChangeDue,
            PaymentMethods = string.Join("; ", o.Payments.Select(p => p.PaymentMethod).Distinct()),
            Currencies     = string.Join("; ", o.Payments.Select(p => p.TenderedCurrency).Distinct())
        });
    }
}
