namespace Tannous.Pos.Application.DTOs.Suppliers;

public class CreateGoodsReceiptDto
{
    public Guid? PurchaseOrderId { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateGoodsReceiptLineDto> Lines { get; set; } = new();
}

public class CreateGoodsReceiptLineDto
{
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
