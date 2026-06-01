using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class OrderLineAddOn : BaseEntity
{
    public decimal Price { get; set; } = 0;
    
    // Foreign keys
    public Guid OrderLineId { get; set; }
    public Guid AddOnId { get; set; }
    
    // Navigation properties
    public virtual OrderLine OrderLine { get; set; } = null!;
    public virtual AddOn AddOn { get; set; } = null!;
}
