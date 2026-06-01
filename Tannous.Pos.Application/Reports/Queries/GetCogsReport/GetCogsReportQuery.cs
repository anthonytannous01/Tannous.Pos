using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetCogsReport;

public class GetCogsReportQuery : IRequest<CogsReportDto>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
