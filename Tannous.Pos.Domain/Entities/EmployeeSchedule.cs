using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A planned work slot for one employee on one day.
/// Created by managers, published to staff.
/// Distinct from <see cref="Shift"/>, which is a cash register session.
/// </summary>
public class EmployeeSchedule : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    /// <summary>UTC.</summary>
    public DateTime ScheduledStart { get; set; }
    /// <summary>UTC.</summary>
    public DateTime ScheduledEnd { get; set; }
    /// <summary>e.g. "Cashier", "Kitchen", "Waiter" — free text, not the Role enum.</summary>
    public string? Position { get; set; }
    public string? Notes { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
