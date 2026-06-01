using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class PurchaseOrderLine : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;
    public string Unit { get; set; } = "pcs";
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid PurchaseOrderId { get; set; }
    public Guid IngredientId { get; set; }
    
    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Ingredient Ingredient { get; set; } = null!;
}
