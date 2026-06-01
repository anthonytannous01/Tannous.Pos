using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class OrderLine : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public decimal TotalPrice { get; set; } = 0;
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
    public virtual ICollection<OrderLineAddOn> OrderLineAddOns { get; set; } = new List<OrderLineAddOn>();
}
