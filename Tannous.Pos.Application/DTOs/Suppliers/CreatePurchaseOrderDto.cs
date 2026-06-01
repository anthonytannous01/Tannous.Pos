namespace Tannous.Pos.Application.DTOs.Suppliers;

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
}

public class CreatePurchaseOrderLineDto
{
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
