using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A named kitchen station (e.g. Grill, Cold Prep, Fry).
/// Menu items are assigned to a station; the KDS can filter by station
/// so each screen shows only relevant items.
/// </summary>
public class KdsStation : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    /// <summary>Hex color for UI display (e.g. "#FF6B35"). Optional.</summary>
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
