using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class WastageRecord : BaseEntity, IAggregateRoot
{
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime WastageDate { get; set; }
    
    // Foreign keys
    public Guid InventoryItemId { get; set; }
    public Guid? BranchId { get; set; }

    // Navigation properties
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
}
