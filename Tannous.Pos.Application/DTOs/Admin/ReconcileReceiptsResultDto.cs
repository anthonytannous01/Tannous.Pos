namespace Tannous.Pos.Application.DTOs.Admin;

public class ReconcileReceiptsResultDto
{
    public int OrdersFixed { get; set; }
    public IReadOnlyList<ReconcileReceiptsItemDto> Results { get; set; } =
        Array.Empty<ReconcileReceiptsItemDto>();
    public int NextReceiptNumber { get; set; }
}

public class ReconcileReceiptsItemDto
{
    public Guid OrderId { get; set; }
    public string? OldReceiptNumber { get; set; }
    public string NewReceiptNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}
