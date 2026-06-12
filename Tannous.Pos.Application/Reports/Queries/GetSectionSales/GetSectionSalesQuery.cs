using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetSectionSales;

public class GetSectionSalesQuery : IRequest<SectionSalesReportDto>
{
    public DateTime From     { get; set; }
    public DateTime To       { get; set; }
    public Guid?    BranchId { get; set; }
}
