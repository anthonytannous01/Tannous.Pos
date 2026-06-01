using MediatR;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id);
        if (order == null)
            throw new ArgumentException($"Order with ID {request.Id} not found");

        order.Status = request.Status;
        order.Notes = request.Notes;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = request.UserId.ToString();

        if (request.Status == Domain.Enums.OrderStatus.Completed)
        {
            order.CompletedDate = DateTime.UtcNow;
        }

        await _orderRepository.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderType = order.OrderType,
            Status = order.Status,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            OrderDate = order.OrderDate,
            CompletedDate = order.CompletedDate,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            Notes = order.Notes,
            CustomerId = order.CustomerId,
            ShiftId = order.ShiftId,
            UserId = order.UserId,
            CreatedAt = order.CreatedAt
        };
    }
}
