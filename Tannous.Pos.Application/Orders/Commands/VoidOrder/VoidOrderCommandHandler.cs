using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Application.Orders;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Orders.Commands.VoidOrder;

public class VoidOrderCommandHandler : IRequestHandler<VoidOrderCommand, OrderDto>
{
    private const string SaleOrderReferencePrefix = "Order-";

    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DbContext _dbContext;
    private readonly ISyncConflictRecorder _syncConflictRecorder;
    private readonly IOperationalAuditRecorder _operationalAuditRecorder;
    private readonly ILogger<VoidOrderCommandHandler> _logger;

    public VoidOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        DbContext dbContext,
        ISyncConflictRecorder syncConflictRecorder,
        IOperationalAuditRecorder operationalAuditRecorder,
        ILogger<VoidOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _syncConflictRecorder = syncConflictRecorder;
        _operationalAuditRecorder = operationalAuditRecorder;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(VoidOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

        if (order.Status != OrderStatus.Open && order.Status != OrderStatus.Paid)
        {
            await _syncConflictRecorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    OperationId = request.IdempotencyKey,
                    EntityType = nameof(Order),
                    EntityId = order.Id,
                    ConflictType = SyncConflictTypes.LifecycleStateConflict,
                    Reason = $"Void rejected: order status is {order.Status}",
                    CorrelationId = request.IdempotencyKey
                },
                cancellationToken);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Reconciliation,
                    Action = OperationalAuditActions.LifecycleStateConflict,
                    EntityType = nameof(Order),
                    EntityId = order.Id,
                    OrderId = order.Id,
                    OperationId = request.IdempotencyKey,
                    CorrelationId = request.IdempotencyKey,
                    Severity = OperationalAuditSeverity.Warning,
                    Summary = $"Void rejected: order status is {order.Status}"
                },
                cancellationToken);

            throw new InvalidOperationException($"Order {request.OrderId} cannot be voided in current status: {order.Status}");
        }

        // Serializable coordinates parallel void/finalize contention with refund uniqueness and reversal idempotency.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await _dbContext.Entry(order).ReloadAsync(cancellationToken);
            if (order.Status == OrderStatus.Void)
            {
                await transaction.CommitAsync(cancellationToken);
                return MapToOrderDto(order);
            }

            if (order.Status != OrderStatus.Open && order.Status != OrderStatus.Paid)
            {
                throw new InvalidOperationException(
                    $"Order {request.OrderId} cannot be voided in current status: {order.Status}");
            }

            if (order.Status == OrderStatus.Paid)
            {
                // GOVERNANCE / RISK: Paid void restores inventory from finalize Sale movements only (no recipe recompute).
                // GOVERNANCE / RISK: Refund rows are internal consistency records only — no external processor; tax row on order is not recomputed (see OrderFinancialTaxGovernance).
                await PersistPaidVoidRefundsAsync(order, request.Reason, request.IdempotencyKey, cancellationToken);
                await ReverseFinalizeInventoryDeductionsAsync(order, request.IdempotencyKey, cancellationToken);
            }

            order.Status = OrderStatus.Void;
            order.Notes = $"VOIDED: {request.Reason}";
            order.ClosedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync(cancellationToken);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Order,
                    Action = OperationalAuditActions.VoidSuccess,
                    EntityType = nameof(Order),
                    EntityId = order.Id,
                    OrderId = order.Id,
                    OperationId = request.IdempotencyKey,
                    CorrelationId = request.IdempotencyKey,
                    Severity = OperationalAuditSeverity.Information,
                    Summary = "Order voided successfully",
                    Metadata = new Dictionary<string, object?> { ["reason"] = request.Reason }
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var affectedTypes = ConcurrencyConflictObservability.FormatAffectedClrTypeNames(ex);
            _logger.LogWarning(
                ex,
                "Refund consistency observability: concurrency conflict during refund. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, AffectedEntityTypes={AffectedEntityTypes}",
                request.OrderId,
                request.IdempotencyKey,
                affectedTypes);
            _logger.LogWarning(
                ex,
                "Inventory reversal observability: concurrency conflict during reversal. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, AffectedEntityTypes={AffectedEntityTypes}",
                request.OrderId,
                request.IdempotencyKey,
                affectedTypes);
            _logger.LogWarning(
                ex,
                "Money-path concurrency visibility: optimistic concurrency conflict during void (RowVersion). OrderId={OrderId}, AffectedEntityTypes={AffectedEntityTypes}",
                request.OrderId,
                affectedTypes);

            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error during void transaction rollback after concurrency conflict. OrderId={OrderId}", request.OrderId);
            }

            await _syncConflictRecorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    OperationId = request.IdempotencyKey,
                    EntityType = nameof(Order),
                    EntityId = request.OrderId,
                    ConflictType = SyncConflictTypes.ConcurrencyConflict,
                    Reason = $"DbUpdateConcurrencyException during void ({affectedTypes})",
                    CorrelationId = request.IdempotencyKey
                },
                cancellationToken);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Concurrency,
                    Action = OperationalAuditActions.ConcurrencyConflict,
                    EntityType = nameof(Order),
                    EntityId = request.OrderId,
                    OrderId = request.OrderId,
                    OperationId = request.IdempotencyKey,
                    CorrelationId = request.IdempotencyKey,
                    Severity = OperationalAuditSeverity.Critical,
                    Summary = $"Concurrency conflict during void ({affectedTypes})"
                },
                cancellationToken);

            throw new InvalidOperationException(
                "Order was modified concurrently. Refresh the order and retry void.");
        }

        return MapToOrderDto(order);
    }

    private async Task PersistPaidVoidRefundsAsync(
        Order order,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existingRefund = await _dbContext.Set<PaymentRefund>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == order.Id, cancellationToken);

        if (existingRefund != null)
        {
            _logger.LogInformation(
                "Refund consistency observability: refund already exists. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}",
                order.Id,
                idempotencyKey,
                existingRefund.Amount);
            _logger.LogInformation(
                "Financial consistency observability: refund already exists. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}",
                order.Id,
                idempotencyKey,
                existingRefund.Amount);
            return;
        }

        var amountTendered = order.AmountTendered > 0
            ? order.AmountTendered
            : order.Payments.Sum(p => p.Amount);
        var refundAmount = OrderSettlementGovernance.ResolveNetCapturedAmountForRefund(order);

        if (refundAmount <= 0)
        {
            _logger.LogInformation(
                "Refund consistency observability: paid void has no net captured amount (skipping refund row). OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
                order.Id,
                idempotencyKey);
            return;
        }

        if (order.ChangeDue > 0)
        {
            _logger.LogInformation(
                "Settlement consistency observability: change due excluded from refund. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, ChangeDue={ChangeDue}, AmountTendered={AmountTendered}, RefundAmount={RefundAmount}",
                order.Id,
                idempotencyKey,
                order.ChangeDue,
                amountTendered,
                refundAmount);
        }

        _logger.LogInformation(
            "Settlement consistency observability: refund uses net captured amount. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}, ChangeDue={ChangeDue}, NetCapturedAmount={NetCapturedAmount}, RefundAmount={RefundAmount}",
            order.Id,
            idempotencyKey,
            amountTendered,
            order.TotalAmount,
            order.ChangeDue,
            order.NetCapturedAmount,
            refundAmount);

        var correlationId = string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"void-order-{order.Id:N}"
            : idempotencyKey;

        _logger.LogInformation(
            "Refund consistency observability: beginning refund persistence. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}",
            order.Id,
            idempotencyKey,
            refundAmount);

        var originalPaymentId = order.Payments.Count == 1 ? order.Payments.First().Id : (Guid?)null;

        var refund = new PaymentRefund
        {
            OrderId = order.Id,
            Amount = refundAmount,
            Reason = reason,
            CorrelationId = correlationId,
            OriginalPaymentId = originalPaymentId
        };

        _dbContext.Set<PaymentRefund>().Add(refund);

        _logger.LogInformation(
            "Refund consistency observability: refund persisted. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}",
            order.Id,
            idempotencyKey,
            refundAmount);

        await _operationalAuditRecorder.RecordAsync(
            new OperationalAuditRecordRequest
            {
                Category = OperationalAuditCategories.Refund,
                Action = OperationalAuditActions.RefundPersisted,
                EntityType = nameof(PaymentRefund),
                EntityId = refund.Id,
                OrderId = order.Id,
                OperationId = idempotencyKey,
                CorrelationId = correlationId,
                Severity = OperationalAuditSeverity.Information,
                Summary = "Paid void refund persisted",
                Metadata = new Dictionary<string, object?> { ["refundAmount"] = refundAmount }
            },
            cancellationToken);

        _logger.LogInformation(
            "Financial consistency observability: refund persisted. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}",
            order.Id,
            idempotencyKey,
            refundAmount);
        _logger.LogInformation(
            "Refund consistency observability: paid void refund reconciliation. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, RefundAmount={RefundAmount}, OrderTotalAmount={OrderTotalAmount}, TaxAmount={TaxAmount}",
            order.Id,
            idempotencyKey,
            refundAmount,
            order.TotalAmount,
            order.TaxAmount);
    }

    private async Task<int> ReverseFinalizeInventoryDeductionsAsync(
        Order order,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var voidReference = BuildVoidReversalReference(order.OrderNumber);
        var existingReversalCount = await _dbContext.Set<InventoryMovement>()
            .CountAsync(
                m => m.Reference == voidReference && m.MovementType == InventoryMovementType.Return,
                cancellationToken);

        if (existingReversalCount > 0)
        {
            _logger.LogInformation(
                "Inventory reversal observability: reversal already completed. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, ReversalMovementCount={ReversalMovementCount}",
                order.Id,
                idempotencyKey,
                existingReversalCount);
            return existingReversalCount;
        }

        var saleReference = BuildSaleReference(order.OrderNumber);
        var originalMovements = await _dbContext.Set<InventoryMovement>()
            .Where(m => m.Reference == saleReference && m.MovementType == InventoryMovementType.Sale)
            .ToListAsync(cancellationToken);

        if (originalMovements.Count == 0)
        {
            _logger.LogInformation(
                "Inventory reversal observability: no finalize sale deductions found for paid void (nothing to reverse). OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
                order.Id,
                idempotencyKey);
            return 0;
        }

        _logger.LogInformation(
            "Inventory reversal observability: beginning paid void reversal. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, SourceSaleMovementCount={SourceSaleMovementCount}",
            order.Id,
            idempotencyKey,
            originalMovements.Count);

        var inventoryItemIds = originalMovements.Select(m => m.InventoryItemId).Distinct().ToList();
        var inventoryItems = await _dbContext.Set<InventoryItem>()
            .Where(ii => inventoryItemIds.Contains(ii.Id))
            .ToDictionaryAsync(ii => ii.Id, cancellationToken);

        var reversalsCreated = 0;
        foreach (var original in originalMovements)
        {
            if (!inventoryItems.TryGetValue(original.InventoryItemId, out var inventoryItem))
            {
                _logger.LogWarning(
                    "Inventory reversal observability: inventory item missing for sale movement during reversal. OrderId={OrderId}, InventoryItemId={InventoryItemId}, MovementId={MovementId}",
                    order.Id,
                    original.InventoryItemId,
                    original.Id);
                continue;
            }

            var restoreQuantity = -original.Quantity;
            if (restoreQuantity <= 0)
            {
                _logger.LogWarning(
                    "Inventory reversal observability: skipping non-deduction sale movement during reversal. OrderId={OrderId}, MovementId={MovementId}, Quantity={Quantity}",
                    order.Id,
                    original.Id,
                    original.Quantity);
                continue;
            }

            var stockBefore = inventoryItem.CurrentStock;
            inventoryItem.CurrentStock += restoreQuantity;
            inventoryItem.LastUpdated = DateTime.UtcNow;

            var reversal = new InventoryMovement
            {
                IngredientId = original.IngredientId,
                InventoryItemId = original.InventoryItemId,
                MovementType = InventoryMovementType.Return,
                Quantity = restoreQuantity,
                UnitCost = original.UnitCost,
                TotalCost = restoreQuantity * original.UnitCost,
                Reference = voidReference,
                ReversedMovementId = original.Id,
                Notes = $"Paid void reversal for order {order.OrderNumber}; reverses movement {original.Id}",
                MovementDate = DateTime.UtcNow
            };

            await _inventoryRepository.AddMovementAsync(reversal);
            reversalsCreated++;

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Inventory,
                    Action = OperationalAuditActions.ReversalMovementPersisted,
                    EntityType = nameof(InventoryMovement),
                    EntityId = reversal.Id,
                    OrderId = order.Id,
                    OperationId = idempotencyKey,
                    CorrelationId = idempotencyKey,
                    Severity = OperationalAuditSeverity.Information,
                    Summary = "Inventory reversal movement persisted for paid void",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["restoredQuantity"] = restoreQuantity,
                        ["reversedMovementId"] = original.Id
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Inventory reversal observability: stock restored after reversal. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, InventoryItemId={InventoryItemId}, IngredientId={IngredientId}, RestoredQuantity={RestoredQuantity}, StockBefore={StockBefore}, StockAfter={StockAfter}",
                order.Id,
                idempotencyKey,
                inventoryItem.Id,
                original.IngredientId,
                restoreQuantity,
                stockBefore,
                inventoryItem.CurrentStock);
        }

        _logger.LogInformation(
            "Inventory reversal observability: reversal movements persisted. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, ReversalMovementCount={ReversalMovementCount}",
            order.Id,
            idempotencyKey,
            reversalsCreated);

        return reversalsCreated;
    }

    private static string BuildSaleReference(string orderNumber) => $"{SaleOrderReferencePrefix}{orderNumber}";

    private static string BuildVoidReversalReference(string orderNumber) => $"{SaleOrderReferencePrefix}{orderNumber}-Void";

    private static OrderDto MapToOrderDto(Order order) =>
        new()
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
