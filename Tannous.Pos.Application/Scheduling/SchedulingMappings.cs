using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling;

/// <summary>
/// Single explicit mapping path for scheduling entities — shared by all handlers
/// so DTO shapes (names, status strings, computed minutes) never diverge.
/// </summary>
public static class SchedulingMappings
{
    public static EmployeeScheduleDto ToDto(EmployeeSchedule schedule) => new()
    {
        Id              = schedule.Id,
        UserId          = schedule.UserId,
        UserFullName    = $"{schedule.User.FirstName} {schedule.User.LastName}",
        UserRole        = schedule.User.Role.ToString(),
        BranchId        = schedule.BranchId,
        ScheduledStart  = schedule.ScheduledStart,
        ScheduledEnd    = schedule.ScheduledEnd,
        Position        = schedule.Position,
        Notes           = schedule.Notes,
        Status          = schedule.Status.ToString(),
        DurationMinutes = (int)(schedule.ScheduledEnd - schedule.ScheduledStart).TotalMinutes
    };

    public static TimeEntryDto ToDto(TimeEntry entry, DateTime utcNow) => new()
    {
        Id            = entry.Id,
        UserId        = entry.UserId,
        UserFullName  = $"{entry.User.FirstName} {entry.User.LastName}",
        BranchId      = entry.BranchId,
        ClockIn       = entry.ClockIn,
        ClockOut      = entry.ClockOut,
        BreakMinutes  = entry.BreakMinutes,
        WorkedMinutes = ComputeWorkedMinutes(entry, utcNow),
        Notes         = entry.Notes,
        Status        = entry.Status.ToString()
    };

    /// <summary>
    /// Completed/adjusted entries: (ClockOut − ClockIn) − break.
    /// Active entries: running total (now − ClockIn) − break. Never negative.
    /// </summary>
    public static int? ComputeWorkedMinutes(TimeEntry entry, DateTime utcNow)
    {
        var end = entry.ClockOut ?? (entry.Status == TimeEntryStatus.Active ? utcNow : (DateTime?)null);
        if (end == null) return null;

        var minutes = (int)(end.Value - entry.ClockIn).TotalMinutes - (entry.BreakMinutes ?? 0);
        return Math.Max(minutes, 0);
    }
}
