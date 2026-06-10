using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Commands.CreateSchedule;

/// <summary>
/// Creates one planned work slot for an employee. Status defaults to Draft.
/// </summary>
public class CreateScheduleCommand : IRequest<EmployeeScheduleDto>
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Position { get; set; }
    public string? Notes { get; set; }
}
