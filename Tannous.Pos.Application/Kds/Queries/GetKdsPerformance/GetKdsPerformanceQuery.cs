using MediatR;
using Tannous.Pos.Application.DTOs.Reports;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsPerformance;

/// <summary>
/// Kitchen performance analytics over completed KDS tickets in a date range.
/// Pure read over existing OrderLine timestamps — no new entities.
/// </summary>
public class GetKdsPerformanceQuery : IRequest<KdsPerformanceDto>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? BranchId { get; set; }
}
