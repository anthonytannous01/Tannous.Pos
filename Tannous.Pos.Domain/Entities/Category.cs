using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Category : BaseEntity, IAggregateRoot
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Arabic category name for customer-facing displays. Optional.</summary>
    public string? NameAr      { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    
    // Navigation properties
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
