using MediatR;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync();

        if (request.Status.HasValue)
        {
            orders = orders.Where(o => o.Status == request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate <= request.EndDate.Value);
        }

        if (request.CustomerId.HasValue)
        {
            orders = orders.Where(o => o.CustomerId == request.CustomerId.Value);
        }

        if (request.ShiftId.HasValue)
        {
            orders = orders.Where(o => o.ShiftId == request.ShiftId.Value);
        }

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderType = o.OrderType,
            Status = o.Status,
            SubTotal = o.SubTotal,
            TaxAmount = o.TaxAmount,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            OrderDate = o.OrderDate,
            CompletedDate = o.CompletedDate,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            Notes = o.Notes,
            CustomerId = o.CustomerId,
            ShiftId = o.ShiftId,
            UserId = o.UserId,
            CreatedAt = o.CreatedAt
        });
    }
}
