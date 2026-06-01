using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class GoodsReceiptLine : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;
    public string Unit { get; set; } = "pcs";
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid GoodsReceiptId { get; set; }
    public Guid IngredientId { get; set; }
    
    // Navigation properties
    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;
    public virtual Ingredient Ingredient { get; set; } = null!;
}
