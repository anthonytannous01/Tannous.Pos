using MediatR;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Application.Orders;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IReceiptNumberService _receiptNumberService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        IReceiptNumberService receiptNumberService,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _receiptNumberService = receiptNumberService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderNumber = await _receiptNumberService.GenerateOrderNumberAsync();

        var order = new Order
        {
            OrderNumber = orderNumber,
            OrderType = request.Order.OrderType,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            CustomerName = request.Order.CustomerName,
            CustomerPhone = request.Order.CustomerPhone,
            Notes = request.Order.Notes,
            CustomerId = request.Order.CustomerId,
            ShiftId = request.ShiftId,
            UserId = request.UserId,
            CreatedBy = request.UserId.ToString()
        };

        decimal subTotal = 0;

        foreach (var lineDto in request.Order.OrderLines)
        {
            var menuItem = await _menuItemRepository.GetByIdAsync(lineDto.MenuItemId);
            if (menuItem == null)
                throw new ArgumentException($"MenuItem with ID {lineDto.MenuItemId} not found");

            var orderLine = new OrderLine
            {
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                TotalPrice = lineDto.UnitPrice * lineDto.Quantity,
                Notes = lineDto.Notes,
                MenuItemId = lineDto.MenuItemId
            };

            order.OrderLines.Add(orderLine);
            subTotal += lineDto.UnitPrice * lineDto.Quantity;
        }

        order.SubTotal = subTotal;
        order.TaxAmount = OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subTotal);
        order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;

        OrderFinancialSnapshotGovernance.LogIfSnapshotViolatesInvariants(
            _logger,
            order.Id,
            order.SubTotal,
            order.TaxAmount,
            order.TotalAmount);

        await _orderRepository.AddAsync(order);
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
