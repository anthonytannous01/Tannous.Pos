namespace Tannous.Pos.Application.DTOs.Inventory;

public class RecipeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid MenuItemId { get; set; }
    public bool IsActive { get; set; }
    public List<RecipeLineDto> RecipeLines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RecipeLineDto
{
    public Guid Id { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityPerItem { get; set; }
    public string Unit { get; set; } = string.Empty;
}
