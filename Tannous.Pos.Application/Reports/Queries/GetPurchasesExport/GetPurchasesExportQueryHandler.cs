using MediatR;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reports.Queries.GetPurchasesExport;

public class GetPurchasesExportQueryHandler
    : IRequestHandler<GetPurchasesExportQuery, IEnumerable<PurchasesExportRowDto>>
{
    private readonly IPurchaseOrderRepository _poRepo;

    public GetPurchasesExportQueryHandler(IPurchaseOrderRepository poRepo) => _poRepo = poRepo;

    public async Task<IEnumerable<PurchasesExportRowDto>> Handle(
        GetPurchasesExportQuery request, CancellationToken ct)
    {
        var all = await _poRepo.GetAllAsync();

        return all
            .Where(po => po.OrderDate >= request.From && po.OrderDate <= request.To)
            .OrderBy(po => po.OrderDate)
            .Select(po => new PurchasesExportRowDto
            {
                Date        = po.OrderDate,
                OrderNumber = po.OrderNumber,
                Supplier    = po.Supplier?.Name ?? string.Empty,
                Status      = po.Status,
                SubTotal    = po.SubTotal,
                TaxAmount   = po.TaxAmount,
                Total       = po.TotalAmount,
                Notes       = po.Notes,
                LineCount   = po.Lines?.Count ?? 0
            });
    }
}
