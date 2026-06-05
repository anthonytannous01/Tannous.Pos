using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class Table : BaseEntity, IAggregateRoot
{
    public string TableNumber { get; set; } = string.Empty; // e.g. "T1", "B3"
    public string? Label { get; set; }                      // optional friendly name
    public int Capacity { get; set; } = 2;
    public TableStatus Status { get; set; } = TableStatus.Available;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Foreign keys
    public Guid FloorPlanId { get; set; }

    // Navigation
    public virtual FloorPlan FloorPlan { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
