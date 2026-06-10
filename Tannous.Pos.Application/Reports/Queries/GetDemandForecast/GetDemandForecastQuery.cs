using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Reports.Queries.GetDemandForecast;

/// <summary>
/// Rule-based demand forecast: same-day-of-week rolling average over a 4-week lookback.
/// Produces estimated orders/revenue, 3-hour time blocks, top items to prep,
/// and ingredient demand derived from recipes. No ML, no external services.
/// </summary>
public class GetDemandForecastQuery : IRequest<DemandForecastDto>
{
    /// <summary>The date you want a forecast for. Defaults to tomorrow UTC.</summary>
    public DateTime? TargetDate { get; set; }
    /// <summary>When set, only orders belonging to this branch are included. Null = all branches.</summary>
    public Guid? BranchId { get; set; }
}
