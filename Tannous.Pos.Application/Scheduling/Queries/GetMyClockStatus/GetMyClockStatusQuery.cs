using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Queries.GetMyClockStatus;

/// <summary>
/// The current Active time entry for a user, or null when not clocked in.
/// </summary>
public class GetMyClockStatusQuery : IRequest<TimeEntryDto?>
{
    public Guid UserId { get; set; }
}
