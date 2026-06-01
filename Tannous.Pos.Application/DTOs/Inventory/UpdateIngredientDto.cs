namespace Tannous.Pos.Application.DTOs.Inventory;

public class UpdateIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostPerUnit { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
