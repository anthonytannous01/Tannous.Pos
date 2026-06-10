using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Actual clock-in / clock-out record for one employee.
/// ClockOut is null while the entry is active (employee is clocked in).
/// </summary>
public class TimeEntry : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    /// <summary>UTC.</summary>
    public DateTime ClockIn { get; set; }
    /// <summary>UTC; null = still clocked in.</summary>
    public DateTime? ClockOut { get; set; }
    public int? BreakMinutes { get; set; }
    public string? Notes { get; set; }
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Active;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
