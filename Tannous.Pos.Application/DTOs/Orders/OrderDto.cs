using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>Lebanon 2025 Budget Law stamp duty applied to this receipt (0 if not applicable).</summary>
    public decimal StampDutyAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new();
}
