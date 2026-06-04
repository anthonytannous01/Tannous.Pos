using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class OrderLine : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public decimal TotalPrice { get; set; } = 0;
    public string? Notes { get; set; }

    /// <summary>
    /// Kitchen Display System status for this line item.
    /// Set to Pending when the order is created; updated by kitchen staff.
    /// </summary>
    public KdsStatus KdsStatus { get; set; } = KdsStatus.Pending;

    /// <summary>UTC timestamp when kitchen acknowledged this line (moved to InProgress).</summary>
    public DateTime? KdsAcknowledgedAt { get; set; }

    /// <summary>UTC timestamp when kitchen marked this line as Done.</summary>
    public DateTime? KdsDoneAt { get; set; }
    
    // Foreign keys
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
    public virtual ICollection<OrderLineAddOn> OrderLineAddOns { get; set; } = new List<OrderLineAddOn>();
}
