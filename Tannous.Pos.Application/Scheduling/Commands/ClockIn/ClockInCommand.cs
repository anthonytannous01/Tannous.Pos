using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockIn;

/// <summary>
/// Clocks an employee in at the given branch. Throws InvalidOperationException
/// (→ 409 via the global handler) when an Active entry already exists for the user.
/// </summary>
public class ClockInCommand : IRequest<TimeEntryDto>
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
}
