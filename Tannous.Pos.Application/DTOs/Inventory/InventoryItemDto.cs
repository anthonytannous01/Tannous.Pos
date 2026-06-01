namespace Tannous.Pos.Application.DTOs.Inventory;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal AverageCost { get; set; }
    public DateTime LastUpdated { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientUnit { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
