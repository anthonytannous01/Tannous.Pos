using MediatR;
using Tannous.Pos.Application.DTOs.Admin;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Admin.Commands.ReconcileReceipts;

public class ReconcileReceiptsCommandHandler : IRequestHandler<ReconcileReceiptsCommand, ReconcileReceiptsResultDto>
{
    private readonly IAdminOrderOperationsRepository _adminOrderOperationsRepository;
    private readonly IAuditService _auditService;

    public ReconcileReceiptsCommandHandler(
        IAdminOrderOperationsRepository adminOrderOperationsRepository,
        IAuditService auditService)
    {
        _adminOrderOperationsRepository = adminOrderOperationsRepository;
        _auditService                   = auditService;
    }

    public async Task<ReconcileReceiptsResultDto> Handle(
        ReconcileReceiptsCommand request,
        CancellationToken cancellationToken)
    {
        var ordersWithoutReceipts = await _adminOrderOperationsRepository
            .GetPaidOrdersWithoutReceiptsAsync(cancellationToken);

        var lastReceiptNumber = await _adminOrderOperationsRepository
            .GetLastAssignedReceiptNumberAsync(cancellationToken);

        // Preserve exact logic from original GetNextReceiptNumberAsync helper
        var nextReceiptNumber = int.TryParse(lastReceiptNumber, out var lastNumber)
            ? lastNumber + 1
            : 1;

        var results = new List<ReconcileReceiptsItemDto>();

        foreach (var order in ordersWithoutReceipts)
        {
            order.ReceiptNumber = nextReceiptNumber.ToString("D6");
            nextReceiptNumber++;

            results.Add(new ReconcileReceiptsItemDto
            {
                OrderId          = order.Id,
                OldReceiptNumber = null,
                NewReceiptNumber = order.ReceiptNumber,
                OrderDate        = order.CreatedAt,
                Total            = order.TotalAmount
            });
        }

        if (ordersWithoutReceipts.Count > 0)
        {
            await _adminOrderOperationsRepository.CommitAsync(cancellationToken);

            await _auditService.LogEventAsync("ReconcileReceipts", "Order", null, new
            {
                OrdersFixed = ordersWithoutReceipts.Count,
                Results     = results
            });
        }

        return new ReconcileReceiptsResultDto
        {
            OrdersFixed       = ordersWithoutReceipts.Count,
            Results           = results,
            NextReceiptNumber = nextReceiptNumber
        };
    }
}
