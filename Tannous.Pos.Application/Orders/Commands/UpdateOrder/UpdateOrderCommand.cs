using MediatR;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Orders.Commands.UpdateOrder;

public class UpdateOrderCommand : IRequest<OrderDto>
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid UserId { get; set; }
}
