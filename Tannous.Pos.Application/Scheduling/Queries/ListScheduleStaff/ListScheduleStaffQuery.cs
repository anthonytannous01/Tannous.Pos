using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;

namespace Tannous.Pos.Application.Scheduling.Queries.ListScheduleStaff;

/// <summary>
/// Returns all active users for the shift-picker (manager / owner only — CanManageShifts).
/// No paging: staff lists are small enough to return in one call.
/// </summary>
public class ListScheduleStaffQuery : IRequest<List<StaffMemberDto>>
{
    public string? Search { get; set; }
}
