using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Commands.CancelSchedule;

/// <summary>
/// Cancels one schedule entry. Allowed from any status except already Cancelled.
/// </summary>
public class CancelScheduleCommand : IRequest<EmployeeScheduleDto>
{
    public Guid ScheduleId { get; set; }
}
