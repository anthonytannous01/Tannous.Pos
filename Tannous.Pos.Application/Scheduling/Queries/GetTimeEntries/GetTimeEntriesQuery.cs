using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Queries.GetTimeEntries;

/// <summary>
/// Time entries with ClockIn in [From, To), newest first.
/// Active entries report a running WorkedMinutes.
/// </summary>
public class GetTimeEntriesQuery : IRequest<List<TimeEntryDto>>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BranchId { get; set; }
}
