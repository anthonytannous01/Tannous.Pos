using MediatR;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Commands.FinalizeOrder;

public class FinalizeOrderCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class PaymentDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}
