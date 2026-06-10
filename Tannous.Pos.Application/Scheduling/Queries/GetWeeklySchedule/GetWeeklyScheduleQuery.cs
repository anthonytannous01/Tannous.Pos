using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Queries.GetWeeklySchedule;

/// <summary>
/// Returns all non-cancelled schedule entries for one calendar week (Monday–Sunday UTC).
/// WeekStart is floored to the Monday of its week.
/// </summary>
public class GetWeeklyScheduleQuery : IRequest<WeeklyScheduleDto>
{
    public DateTime WeekStart { get; set; }
    public Guid? BranchId { get; set; }
}
