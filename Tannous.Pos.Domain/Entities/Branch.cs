using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A physical or logical branch of the business (e.g. "Main Branch", "Dekwaneh Outlet").
/// All operational data (Orders, Shifts, Inventory, etc.) is scoped to a Branch.
/// </summary>
public class Branch : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
