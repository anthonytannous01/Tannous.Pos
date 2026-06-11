using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Supplier : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<Ingredient> PreferredIngredients { get; set; } = new List<Ingredient>();
}
