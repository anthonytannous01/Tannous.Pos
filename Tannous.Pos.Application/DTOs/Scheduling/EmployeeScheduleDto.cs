namespace Tannous.Pos.Application.DTOs.Scheduling;

public class EmployeeScheduleDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Position { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class WeeklyScheduleDto
{
    /// <summary>Monday 00:00 UTC.</summary>
    public DateTime WeekStart { get; set; }
    /// <summary>Sunday 23:59 UTC.</summary>
    public DateTime WeekEnd { get; set; }
    public List<EmployeeScheduleDto> Schedules { get; set; } = new();
}
