namespace Tannous.Pos.Application.DTOs.Scheduling;

public class TimeEntryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public int? BreakMinutes { get; set; }
    /// <summary>
    /// Worked minutes net of break. For active entries this is the running total
    /// (now − ClockIn − break); for completed entries it is final.
    /// </summary>
    public int? WorkedMinutes { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
}
