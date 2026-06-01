using MediatR;
using Tannous.Pos.Application.Interfaces;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Admin.Commands.ReprintReceipt;

public class ReprintReceiptCommandHandler : IRequestHandler<ReprintReceiptCommand, ReprintReceiptResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IAuditService _auditService;
    private readonly IPrintingService _printingService;

    public ReprintReceiptCommandHandler(
        IOrderRepository orderRepository,
        IAuditService auditService,
        IPrintingService printingService)
    {
        _orderRepository = orderRepository;
        _auditService    = auditService;
        _printingService = printingService;
    }

    public async Task<ReprintReceiptResult> Handle(ReprintReceiptCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId);

        if (order == null)
            return new ReprintReceiptResult { Found = false };

        if (string.IsNullOrEmpty(order.ReceiptNumber))
            return new ReprintReceiptResult { Found = true, HasReceiptNumber = false };

        var receipt = await _printingService.RenderReceiptAsync(request.OrderId, 42);

        await _auditService.LogEventAsync("ReprintReceipt", "Order", request.OrderId, new
        {
            ReceiptNumber = order.ReceiptNumber,
            ReprintTime   = DateTime.UtcNow
        });

        return new ReprintReceiptResult { Found = true, HasReceiptNumber = true, Receipt = receipt };
    }
}
