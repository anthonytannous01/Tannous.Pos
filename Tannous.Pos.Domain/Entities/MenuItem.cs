using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class MenuItem : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasAddOns { get; set; }
    public bool HasIngredients { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public byte[] Version { get; set; } = new byte[8]; // Concurrency token for sync conflicts
    
    // Foreign keys
    public Guid CategoryId { get; set; }
    
    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<AddOn> AddOns { get; set; } = new List<AddOn>();
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}
