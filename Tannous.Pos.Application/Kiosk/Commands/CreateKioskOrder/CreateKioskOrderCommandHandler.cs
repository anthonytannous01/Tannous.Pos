using MediatR;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Application.Orders;

namespace Tannous.Pos.Application.Kiosk.Commands.CreateKioskOrder;

/// <summary>
/// Creates an unauthenticated kiosk order (no shift, no user).
/// Order is placed in Pending status — staff finalizes at the counter.
/// </summary>
public class CreateKioskOrderCommandHandler
    : IRequestHandler<CreateKioskOrderCommand, KioskOrderResultDto>
{
    private readonly IOrderRepository           _orderRepo;
    private readonly IReceiptNumberService      _receiptNumberService;
    private readonly IBusinessSettingsRepository _settingsRepo;
    private readonly IBranchRepository          _branchRepo;
    private readonly IUnitOfWork               _unitOfWork;

    public CreateKioskOrderCommandHandler(
        IOrderRepository            orderRepo,
        IReceiptNumberService       receiptNumberService,
        IBusinessSettingsRepository settingsRepo,
        IBranchRepository           branchRepo,
        IUnitOfWork                 unitOfWork)
    {
        _orderRepo            = orderRepo;
        _receiptNumberService = receiptNumberService;
        _settingsRepo         = settingsRepo;
        _branchRepo           = branchRepo;
        _unitOfWork           = unitOfWork;
    }

    public async Task<KioskOrderResultDto> Handle(
        CreateKioskOrderCommand request, CancellationToken ct)
    {
        if (!request.Lines.Any())
            throw new ArgumentException("Kiosk order must have at least one item.");

        var settings = await _settingsRepo.GetAsync(ct);
        var branchId = (await _branchRepo.GetDefaultAsync(ct))?.Id;
        var orderNumber = await _receiptNumberService.GenerateOrderNumberAsync();

        var subTotal = request.Lines.Sum(l => l.UnitPrice * l.Quantity);

        // Shared with CreateOrder and FinalizeOrder so every order path agrees on tax.
        // Kiosk orders have no settings-null fallback case in practice, but the helper covers it.
        var taxAmount = OrderFinancialGovernance.ComputeTaxOnSubtotal(subTotal, settings);

        var total = subTotal + taxAmount;

        var order = new Order
        {
            OrderNumber   = orderNumber,
            OrderType     = OrderType.Takeaway,
            Status        = OrderStatus.Pending,
            OrderDate     = DateTime.UtcNow,
            CustomerName  = request.CustomerName?.Trim(),
            Notes         = request.Notes?.Trim(),
            SubTotal      = subTotal,
            TaxAmount     = taxAmount,
            TotalAmount   = total,
            BranchId      = branchId,
            CreatedBy     = "kiosk"
        };

        foreach (var line in request.Lines)
        {
            order.OrderLines.Add(new OrderLine
            {
                MenuItemId = line.MenuItemId,
                Quantity   = line.Quantity,
                UnitPrice  = line.UnitPrice,
                TotalPrice = line.UnitPrice * line.Quantity
            });
        }

        await _orderRepo.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return new KioskOrderResultDto
        {
            OrderId     = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = total,
            Currency    = settings?.Currency ?? "USD"
        };
    }
}
