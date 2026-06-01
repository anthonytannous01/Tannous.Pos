using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetEodReport;

public class GetEodReportQuery : IRequest<EodReportDto>
{
    /// <summary>The UTC date to report on. Defaults to today if null.</summary>
    public DateTime? Date { get; set; }
}
