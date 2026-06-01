namespace Tannous.Pos.Application.DTOs.Inventory;

public class UpdateRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid MenuItemId { get; set; }
    public List<UpdateRecipeLineDto> Lines { get; set; } = new();
}

public class UpdateRecipeLineDto
{
    public Guid IngredientId { get; set; }
    public decimal QuantityPerItem { get; set; }
}
