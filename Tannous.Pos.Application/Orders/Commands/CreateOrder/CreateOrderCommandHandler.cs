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
    private readonly IShiftRepository _shiftRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IBusinessSettingsRepository _businessSettingsRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        IReceiptNumberService receiptNumberService,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger,
        IShiftRepository shiftRepository,
        IBranchRepository branchRepository,
        IBusinessSettingsRepository businessSettingsRepository)
    {
        _orderRepository   = orderRepository;
        _menuItemRepository = menuItemRepository;
        _receiptNumberService = receiptNumberService;
        _unitOfWork         = unitOfWork;
        _logger             = logger;
        _shiftRepository    = shiftRepository;
        _branchRepository   = branchRepository;
        _businessSettingsRepository = businessSettingsRepository;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderNumber = await _receiptNumberService.GenerateOrderNumberAsync();

        // Resolve ShiftId: explicit (sync/offline payload) → the caller's open shift.
        // The online create path (OrdersController POST) supplies no ShiftId, so without this
        // fallback orders are never linked to a shift and drawer math (expected cash) never
        // sees their cash payments.
        var shiftId = request.ShiftId;
        if (shiftId == null)
        {
            var openShift = await _shiftRepository.GetOpenShiftByUserAsync(request.UserId);
            shiftId = openShift?.Id;
            if (shiftId == null)
            {
                _logger.LogWarning(
                    "Order created with no shift linkage: no explicit ShiftId and no open shift for user. UserId={UserId}, OrderNumber={OrderNumber}",
                    request.UserId,
                    orderNumber);
            }
        }

        // Resolve BranchId: explicit → from active shift → default branch
        var branchId = request.BranchId;
        if (branchId == null && shiftId.HasValue)
        {
            var shift = await _shiftRepository.GetByIdAsync(shiftId.Value);
            branchId = shift?.BranchId;
        }
        if (branchId == null)
        {
            branchId = (await _branchRepository.GetDefaultAsync(cancellationToken))?.Id;
        }

        var order = new Order
        {
            OrderNumber   = orderNumber,
            OrderType     = request.Order.OrderType,
            Status        = OrderStatus.Pending,
            OrderDate     = DateTime.UtcNow,
            CustomerName  = request.Order.CustomerName,
            CustomerPhone = request.Order.CustomerPhone,
            Notes         = request.Order.Notes,
            CustomerId    = request.Order.CustomerId,
            TableId       = request.Order.TableId,
            ShiftId       = shiftId,
            UserId        = request.UserId,
            BranchId      = branchId,
            CreatedBy     = request.UserId.ToString()
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

        // Tax must come from configuration here, not a fixed rate. Finalize already computes
        // from BusinessSettings, so a hardcoded value at create time makes an open order display
        // tax that vanishes when the order is finalized.
        var businessSettings = await _businessSettingsRepository.GetAsync(cancellationToken);
        order.TaxAmount = OrderFinancialGovernance.ComputeTaxOnSubtotal(subTotal, businessSettings);
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
