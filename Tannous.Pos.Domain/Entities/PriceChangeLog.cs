using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class PriceChangeLog : BaseEntity, IAggregateRoot
{
    public decimal OldPrice { get; set; } = 0;
    public decimal NewPrice { get; set; } = 0;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ChangeDate { get; set; }
    
    // Foreign keys
    public Guid MenuItemId { get; set; }
    public Guid UserId { get; set; }
    
    // Navigation properties
    public virtual MenuItem MenuItem { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
