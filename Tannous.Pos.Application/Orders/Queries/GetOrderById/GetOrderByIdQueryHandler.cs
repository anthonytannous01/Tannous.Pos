using MediatR;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.Id);
        if (order == null)
            return null;

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderType = order.OrderType,
            Status = order.Status,
            CustomerId = order.CustomerId,
            ShiftId = order.ShiftId,
            UserId = order.UserId,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            OrderLines = order.OrderLines.Select(ol => new OrderLineDto
            {
                MenuItemId = ol.MenuItemId,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                Notes = ol.Notes,
                AddOns = ol.OrderLineAddOns.Select(ola => new OrderLineAddOnDto
                {
                    AddOnId = ola.AddOnId,
                    Price = ola.Price
                }).ToList()
            }).ToList()
        };
    }
}
