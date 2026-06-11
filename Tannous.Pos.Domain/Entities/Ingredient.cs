using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class Ingredient : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal CostPerUnit { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public decimal MinimumStock { get; set; }
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// The supplier this ingredient is typically ordered from.
    /// Used by the supplier intelligence engine to group purchase order suggestions.
    /// Null means no preferred supplier assigned yet.
    /// </summary>
    public Guid? PreferredSupplierId { get; set; }
    
    // Navigation properties
    public virtual Supplier? PreferredSupplier { get; set; }
    public virtual ICollection<RecipeLine> RecipeLines { get; set; } = new List<RecipeLine>();
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
