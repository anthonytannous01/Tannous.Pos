using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class GoodsReceipt : BaseEntity, IAggregateRoot
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid? PurchaseOrderId { get; set; }
    
    // Navigation properties
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}
