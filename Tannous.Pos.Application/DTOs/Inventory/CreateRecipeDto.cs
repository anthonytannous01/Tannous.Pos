namespace Tannous.Pos.Application.DTOs.Inventory;

public class CreateRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid MenuItemId { get; set; }
    public List<CreateRecipeLineDto> Lines { get; set; } = new();
}

public class CreateRecipeLineDto
{
    public Guid IngredientId { get; set; }
    public decimal QuantityPerItem { get; set; }
}
