using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Commands.UpdateSchedule;

/// <summary>
/// Updates a Draft schedule entry. Published/Cancelled entries are immutable
/// (404 if missing, 409 if not Draft — mapped by the global exception handler).
/// </summary>
public class UpdateScheduleCommand : IRequest<EmployeeScheduleDto>
{
    public Guid ScheduleId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Position { get; set; }
    public string? Notes { get; set; }
}
