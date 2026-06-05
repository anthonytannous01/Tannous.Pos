using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class InventoryMovement : BaseEntity, IAggregateRoot
{
    public InventoryMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    /// <summary>When set, links a void/reversal movement to the original finalize deduction movement.</summary>
    public Guid? ReversedMovementId { get; set; }
    public DateTime MovementDate { get; set; }
    
    // Foreign keys
    public Guid IngredientId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid? BranchId { get; set; }

    // Navigation properties
    public virtual Ingredient Ingredient { get; set; } = null!;
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
}
