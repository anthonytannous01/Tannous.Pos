using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Orders;

public class CreateOrderDto
{
    public OrderType OrderType { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public Guid? CustomerId { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new List<OrderLineDto>();
}

public class OrderLineDto
{
    public Guid MenuItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public List<Guid> AddOnIds { get; set; } = new List<Guid>();
    public List<OrderLineAddOnDto> AddOns { get; set; } = new();
}
