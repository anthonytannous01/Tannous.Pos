using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A named zone on the floor plan (e.g. "Indoor", "Terrace", "Bar").
/// Tables belong to a floor plan.
/// </summary>
public class FloorPlan : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
}
