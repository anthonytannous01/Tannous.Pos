using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetSalesExport;

public class GetSalesExportQuery : IRequest<IEnumerable<SalesExportRowDto>>
{
    public DateTime  From     { get; set; }
    public DateTime  To       { get; set; }
    public Guid?     BranchId { get; set; }
}
