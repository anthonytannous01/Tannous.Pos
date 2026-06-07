using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetPurchasesExport;

public class GetPurchasesExportQuery : IRequest<IEnumerable<PurchasesExportRowDto>>
{
    public DateTime From { get; set; }
    public DateTime To   { get; set; }
}
