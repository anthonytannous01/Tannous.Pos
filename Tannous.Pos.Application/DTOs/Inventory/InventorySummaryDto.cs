namespace Tannous.Pos.Application.DTOs.Inventory;

public class InventorySummaryDto
{
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal OnHand { get; set; }
}
