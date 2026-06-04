using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetSalesSummary;

/// <summary>
/// Returns a real-time sales summary for the owner dashboard.
/// Defaults to today (UTC midnight → now) when no range is supplied.
/// </summary>
public class GetSalesSummaryQuery : IRequest<SalesSummaryDto>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
