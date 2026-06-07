using MediatR;

namespace Tannous.Pos.Application.Kiosk.Commands.CreateKioskOrder;

public class CreateKioskOrderCommand : IRequest<KioskOrderResultDto>
{
    public string? CustomerName { get; set; }
    public string? Notes        { get; set; }
    public List<KioskOrderLineDto> Lines { get; set; } = new();
}

public class KioskOrderLineDto
{
    public Guid    MenuItemId { get; set; }
    public int     Quantity   { get; set; } = 1;
    public decimal UnitPrice  { get; set; }
}

public class KioskOrderResultDto
{
    public Guid    OrderId     { get; set; }
    public string  OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string  Currency    { get; set; } = "USD";
    public string  Message     { get; set; } = "Your order has been placed. Please wait for your order number to be called.";
}
