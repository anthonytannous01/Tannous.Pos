using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class AddOn : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public byte[] Version { get; set; } = new byte[8]; // Concurrency token for sync conflicts
    
    // Navigation properties
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public virtual ICollection<OrderLineAddOn> OrderLineAddOns { get; set; } = new List<OrderLineAddOn>();
}
