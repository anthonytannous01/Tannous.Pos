using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockOut;

/// <summary>
/// Clocks an employee out of their Active time entry. Throws KeyNotFoundException
/// (→ 404 via the global handler) when no Active entry exists.
/// </summary>
public class ClockOutCommand : IRequest<TimeEntryDto>
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public int? BreakMinutes { get; set; }
    public string? Notes { get; set; }
}
