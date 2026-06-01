using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class RecipeLine : BaseEntity
{
    public decimal QuantityPerItem { get; set; }
    public string Unit { get; set; } = "pcs";
    
    // Foreign keys
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
    
    // Navigation properties
    public virtual Recipe Recipe { get; set; } = null!;
    public virtual Ingredient Ingredient { get; set; } = null!;
}
