using MediatR;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<OrderDto>
{
    public CreateOrderDto Order { get; set; } = new CreateOrderDto();
    public Guid UserId { get; set; }
    public Guid? ShiftId { get; set; }
}
