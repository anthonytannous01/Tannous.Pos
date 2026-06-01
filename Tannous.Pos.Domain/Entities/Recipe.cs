using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Recipe : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int Yield { get; set; } = 1;
    public string YieldUnit { get; set; } = "serving";
    
    // Foreign keys
    public Guid MenuItemId { get; set; }
    
    // Navigation properties
    public virtual MenuItem MenuItem { get; set; } = null!;
    public virtual ICollection<RecipeLine> RecipeLines { get; set; } = new List<RecipeLine>();
}
