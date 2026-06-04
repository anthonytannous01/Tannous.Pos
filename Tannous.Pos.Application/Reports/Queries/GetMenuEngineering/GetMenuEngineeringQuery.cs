using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetMenuEngineering;

public class GetMenuEngineeringQuery : IRequest<MenuEngineeringReportDto>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
