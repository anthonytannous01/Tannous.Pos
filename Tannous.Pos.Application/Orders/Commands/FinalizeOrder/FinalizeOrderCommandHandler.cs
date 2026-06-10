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
// Table and TableStatus are in Domain.Entities and Domain.Enums respectively (already imported above)

namespace Tannous.Pos.Application.Orders.Commands.FinalizeOrder;

/// <summary>
/// Handles order finalization with full transactional safety.
/// This handler ensures atomicity: either all operations (payments, status updates, inventory movements) 
/// commit together, or nothing commits. This prevents partial finalized orders, orphan payments, 
/// or inconsistent inventory states.
/// </summary>
public class FinalizeOrderCommandHandler : IRequestHandler<FinalizeOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReceiptNumberService _receiptNumberService;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IBusinessSettingsRepository _businessSettingsRepository;
    private readonly DbContext _dbContext;
    private readonly ISyncConflictRecorder _syncConflictRecorder;
    private readonly IOperationalAuditRecorder _operationalAuditRecorder;
    private readonly ILogger<FinalizeOrderCommandHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IWebhookDispatcher _webhookDispatcher;

    public FinalizeOrderCommandHandler(
        IOrderRepository orderRepository,
        IReceiptNumberService receiptNumberService,
        IRecipeRepository recipeRepository,
        IInventoryRepository inventoryRepository,
        IBusinessSettingsRepository businessSettingsRepository,
        DbContext dbContext,
        ISyncConflictRecorder syncConflictRecorder,
        IOperationalAuditRecorder operationalAuditRecorder,
        ILogger<FinalizeOrderCommandHandler> logger,
        INotificationService notificationService,
        IWebhookDispatcher webhookDispatcher)
    {
        _orderRepository = orderRepository;
        _receiptNumberService = receiptNumberService;
        _recipeRepository = recipeRepository;
        _inventoryRepository = inventoryRepository;
        _businessSettingsRepository = businessSettingsRepository;
        _dbContext = dbContext;
        _syncConflictRecorder = syncConflictRecorder;
        _operationalAuditRecorder = operationalAuditRecorder;
        _logger = logger;
        _notificationService = notificationService;
        _webhookDispatcher = webhookDispatcher;
    }

    public async Task<OrderDto> Handle(FinalizeOrderCommand request, CancellationToken cancellationToken)
    {
        // Use explicit transaction to ensure atomicity of all operations:
        // - Order status update
        // - Payment creation
        // - Receipt number assignment
        // - Inventory movements
        // If any operation fails, the entire transaction rolls back, preventing partial state.
        // When invoked under sync durable replay, the outer EF transaction is already open — join it so
        // finalize persistence and SyncOperationReceipt commit atomically (no nested BeginTransaction).
        var joinsOuterTransaction = _dbContext.Database.CurrentTransaction != null;
        if (joinsOuterTransaction)
        {
            _logger.LogInformation(
                "Order finalization joining existing database transaction (sync durable replay outer scope). OrderId: {OrderId}, IdempotencyKey: {IdempotencyKey}",
                request.OrderId,
                request.IdempotencyKey);
        }

        IDbContextTransaction? ownedTransaction = null;
        if (!joinsOuterTransaction)
        {
            // Serializable prevents cross-request finalize races from duplicating payments under parallel idempotency keys.
            ownedTransaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        }

        try
        {
            _logger.LogInformation(
                "Starting order finalization transaction. OrderId: {OrderId}, IdempotencyKey: {IdempotencyKey}",
                request.OrderId,
                request.IdempotencyKey);

            // Load order for finalize without tracking MenuItem/AddOn aggregates (they carry separate Version tokens).
            var order = await _dbContext.Set<Order>()
                .Include(o => o.OrderLines)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order not found. OrderId: {OrderId}", request.OrderId);
                throw new InvalidOperationException($"Order {request.OrderId} not found");
            }

            // Idempotency check: If order is already finalized, return existing state
            // This handles duplicate finalize requests (e.g., network retries, UI double-clicks)
            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation(
                    "Order already finalized. Returning existing state. OrderId: {OrderId}, ReceiptNumber: {ReceiptNumber}",
                    order.Id,
                    order.ReceiptNumber);

                _logger.LogInformation(
                    "Finalize governance: short-circuit on Paid order (no duplicate inventory/payments in this path). OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
                    order.Id,
                    request.IdempotencyKey);

                // GOVERNANCE / RISK: idempotency / replay observability only — same business outcome as prior finalize; no retry orchestration.
                _logger.LogWarning(
                    "Finalize idempotency observability: duplicate finalize or replay attempt; order already Paid (short-circuit, stable response). OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
                    order.Id,
                    request.IdempotencyKey);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Order,
                        Action = OperationalAuditActions.FinalizeReplayShortCircuit,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        OrderId = order.Id,
                        OperationId = request.IdempotencyKey,
                        CorrelationId = request.IdempotencyKey,
                        Severity = OperationalAuditSeverity.Information,
                        Summary = "Finalize replay short-circuit on already Paid order"
                    },
                    cancellationToken);

                // Return existing finalized order state
                return MapToOrderDto(order);
            }

            if (order.Status == OrderStatus.Void)
            {
                await _syncConflictRecorder.RecordAsync(
                    new SyncConflictRecordRequest
                    {
                        OperationId = request.IdempotencyKey,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        ConflictType = SyncConflictTypes.StaleOfflineMutation,
                        Reason = "Finalize attempted on void order (stale offline mutation risk)",
                        CorrelationId = request.IdempotencyKey
                    },
                    cancellationToken);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Reconciliation,
                        Action = OperationalAuditActions.StaleOfflineMutation,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        OrderId = order.Id,
                        OperationId = request.IdempotencyKey,
                        CorrelationId = request.IdempotencyKey,
                        Severity = OperationalAuditSeverity.Warning,
                        Summary = "Finalize attempted on void order (stale offline mutation)"
                    },
                    cancellationToken);

                throw new InvalidOperationException(
                    $"Order {request.OrderId} cannot be finalized because it is void.");
            }

            // Validate order is in a finalizable state (Open or Pending from create path).
            if (order.Status is not (OrderStatus.Open or OrderStatus.Pending))
            {
                await _syncConflictRecorder.RecordAsync(
                    new SyncConflictRecordRequest
                    {
                        OperationId = request.IdempotencyKey,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        ConflictType = SyncConflictTypes.LifecycleStateConflict,
                        Reason = $"Finalize rejected: order status is {order.Status}",
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
                        Summary = $"Finalize rejected: order status is {order.Status}"
                    },
                    cancellationToken);

                _logger.LogWarning(
                    "Order is not in a finalizable status. OrderId: {OrderId}, CurrentStatus: {Status}",
                    order.Id,
                    order.Status);
                throw new InvalidOperationException(
                    $"Order {request.OrderId} is not in a finalizable status. Current status: {order.Status}");
            }

            // Calculate totals from order lines and add-ons
            var subTotal = order.OrderLines.Sum(ol => ol.TotalPrice) +
                          order.OrderLines.SelectMany(ol => ol.OrderLineAddOns).Sum(ola => ola.Price);

            if (order.SubTotal != 0 && Math.Abs(order.SubTotal - subTotal) > 0.01m)
            {
                _logger.LogWarning(
                    "Order subtotal recomputed from lines differs from persisted SubTotal. Persisted={Persisted}, Recomputed={Recomputed}, OrderId={OrderId}. Investigate pricing drift between create/update and finalize.",
                    order.SubTotal,
                    subTotal,
                    order.Id);
            }

            // Use the configured tax rate from BusinessSettings rather than the legacy
            // hardcoded 10% constant — ensures the finalize path matches receipt printing.
            // Fall back to LegacyOrderFlowTaxRate if settings are unavailable (e.g. first boot).
            var businessSettings = await _businessSettingsRepository.GetAsync(cancellationToken);

            // Tax computation: use configured BusinessSettings.TaxRate when available (e.g. 11% Lebanon VAT).
            // Fall back to OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal (10% fixed) when settings
            // are unavailable (first boot) — preserves the legacy order flow tax anchor.
            var taxAmount = businessSettings?.TaxRate > 0
                ? decimal.Round(subTotal * (businessSettings.TaxRate / 100m), 28, MidpointRounding.AwayFromZero)
                : OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subTotal);

            // Stamp duty (Lebanon 2025 Budget Law): $2 USD on USD-denominated receipts.
            // Applied only when StampDutyEnabled = true in BusinessSettings.
            var stampDuty = 0m;
            if (businessSettings?.StampDutyEnabled == true && businessSettings.StampDutyAmountUsd > 0)
            {
                // Apply stamp duty when at least one payment is tendered in USD.
                var hasUsdPayment = request.Payments.Any(p =>
                    string.IsNullOrEmpty(p.TenderedCurrency) ||
                    p.TenderedCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase));
                if (hasUsdPayment)
                    stampDuty = businessSettings.StampDutyAmountUsd;
            }

            var totalAmount = subTotal + taxAmount + stampDuty;

            OrderFinancialSnapshotGovernance.LogIfSnapshotViolatesInvariants(
                _logger,
                order.Id,
                subTotal,
                taxAmount,
                totalAmount);

            if (order.DiscountAmount > 0)
            {
                _logger.LogWarning(
                    "Order has DiscountAmount={Discount} but finalize payment math uses subtotal+tax only (discount not applied in this path). OrderId: {OrderId}",
                    order.DiscountAmount,
                    order.Id);
            }

            // Validate payments and settlement (amount owed vs tendered vs change due vs net captured).
            var totalPayments = request.Payments.Sum(p => p.Amount);

            if (totalPayments < totalAmount)
            {
                _logger.LogWarning(
                    "Settlement consistency observability: underpayment rejected. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}, ChangeDue={ChangeDue}, NetCapturedAmount={NetCapturedAmount}",
                    order.Id,
                    request.IdempotencyKey,
                    totalPayments,
                    totalAmount,
                    0m,
                    0m);
                _logger.LogWarning(
                    "Insufficient payment amount. OrderId: {OrderId}, Required: {Required}, Provided: {Provided}",
                    order.Id,
                    totalAmount,
                    totalPayments);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Settlement,
                        Action = OperationalAuditActions.SettlementUnderpaymentRejected,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        OrderId = order.Id,
                        OperationId = request.IdempotencyKey,
                        CorrelationId = request.IdempotencyKey,
                        Severity = OperationalAuditSeverity.Warning,
                        Summary = "Settlement underpayment rejected during finalize",
                        Metadata = new Dictionary<string, object?>
                        {
                            ["requiredAmount"] = totalAmount,
                            ["providedAmount"] = totalPayments
                        }
                    },
                    cancellationToken);

                throw new InvalidOperationException(
                    $"Insufficient payment amount. Required: {totalAmount}, Provided: {totalPayments}");
            }

            decimal changeDue;
            decimal netCaptured;
            if (totalPayments > totalAmount)
            {
                changeDue = totalPayments - totalAmount;
                netCaptured = totalAmount;
                _logger.LogInformation(
                    "Settlement consistency observability: overpayment with change due. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}, ChangeDue={ChangeDue}, NetCapturedAmount={NetCapturedAmount}",
                    order.Id,
                    request.IdempotencyKey,
                    totalPayments,
                    totalAmount,
                    changeDue,
                    netCaptured);
                _logger.LogWarning(
                    "Financial consistency observability: overpayment detected. OrderId={OrderId}, PaidAmount={PaidAmount}, ExpectedAmount={ExpectedAmount}, Difference={Difference}",
                    order.Id,
                    totalPayments,
                    totalAmount,
                    totalPayments - totalAmount);
                _logger.LogWarning(
                    "Payment total exceeds required amount (overpayment / change due recorded on order). OrderId: {OrderId}, Required: {Required}, Provided: {Provided}",
                    order.Id,
                    totalAmount,
                    totalPayments);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Settlement,
                        Action = OperationalAuditActions.SettlementOverpayment,
                        EntityType = nameof(Order),
                        EntityId = order.Id,
                        OrderId = order.Id,
                        OperationId = request.IdempotencyKey,
                        CorrelationId = request.IdempotencyKey,
                        Severity = OperationalAuditSeverity.Information,
                        Summary = "Settlement overpayment with change due recorded",
                        Metadata = new Dictionary<string, object?>
                        {
                            ["totalAmount"] = totalAmount,
                            ["paidAmount"] = totalPayments,
                            ["changeDue"] = changeDue,
                            ["netCaptured"] = netCaptured
                        }
                    },
                    cancellationToken);
            }
            else
            {
                changeDue = 0m;
                netCaptured = totalPayments;
                _logger.LogInformation(
                    "Settlement consistency observability: exact payment. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}, ChangeDue={ChangeDue}, NetCapturedAmount={NetCapturedAmount}",
                    order.Id,
                    request.IdempotencyKey,
                    totalPayments,
                    totalAmount,
                    changeDue,
                    netCaptured);
            }

            if (await _dbContext.Set<Payment>().AnyAsync(p => p.OrderId == order.Id, cancellationToken))
            {
                await _dbContext.Entry(order).ReloadAsync(cancellationToken);
                if (order.Status == OrderStatus.Paid)
                {
                    _logger.LogInformation(
                        "Finalize governance: payment rows already exist; returning Paid order without duplicate payment insert. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
                        order.Id,
                        request.IdempotencyKey);
                    return MapToOrderDto(order);
                }

                throw new InvalidOperationException(
                    "Order already has payment rows recorded. Refresh the order and retry finalize.");
            }

            // Generate receipt number (within transaction to ensure uniqueness)
            var receiptNumber = await _receiptNumberService.GenerateReceiptNumberAsync();

            // Create payments (all within the same transaction)
            var exchangeRate = businessSettings?.ExchangeRateLbpPerUsd ?? 0m;
            foreach (var paymentDto in request.Payments)
            {
                var tenderedCurrency = string.IsNullOrWhiteSpace(paymentDto.TenderedCurrency)
                    ? "USD"
                    : paymentDto.TenderedCurrency.ToUpperInvariant();

                // Normalise amount to USD for reporting
                decimal amountInUsd;
                decimal? exchangeRateUsed = null;
                if (tenderedCurrency == "LBP" && exchangeRate > 0)
                {
                    amountInUsd = decimal.Round(paymentDto.Amount / exchangeRate, 4, MidpointRounding.AwayFromZero);
                    exchangeRateUsed = exchangeRate;
                }
                else
                {
                    amountInUsd = paymentDto.Amount; // already USD
                }

                var payment = new Payment
                {
                    OrderId          = order.Id,
                    Amount           = paymentDto.Amount,
                    PaymentMethod    = paymentDto.PaymentMethod,
                    TransactionId    = paymentDto.TransactionId,
                    Notes            = paymentDto.Notes,
                    PaymentDate      = DateTime.UtcNow,
                    TenderedCurrency = tenderedCurrency,
                    ExchangeRateUsed = exchangeRateUsed,
                    AmountInUsd      = amountInUsd
                };
                await _dbContext.Set<Payment>().AddAsync(payment, cancellationToken);
            }

            // Update order status and totals (within transaction)
            order.Status = OrderStatus.Paid;
            order.SubTotal = subTotal;
            order.TaxAmount = taxAmount;
            order.StampDutyAmount = stampDuty;
            order.TotalAmount = totalAmount;
            order.AmountTendered = totalPayments;
            order.ChangeDue = changeDue;
            order.NetCapturedAmount = netCaptured;
            order.ReceiptNumber = receiptNumber;
            order.ClosedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Settlement consistency observability: settlement persisted. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}, ChangeDue={ChangeDue}, NetCapturedAmount={NetCapturedAmount}",
                order.Id,
                request.IdempotencyKey,
                order.AmountTendered,
                order.TotalAmount,
                order.ChangeDue,
                order.NetCapturedAmount);

            // Create inventory movements for recipe ingredients (within same transaction)
            // This deducts stock based on menu item recipes and order line quantities.
            // Note: Only menu items with recipes are processed. Add-ons do not affect inventory in v1.
            await CreateInventoryDeductionsAsync(order, request.IdempotencyKey, cancellationToken);

            // Save all changes atomically (order, payments, inventory movements, stock updates)
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (!joinsOuterTransaction)
            {
                await ownedTransaction!.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Order finalization completed successfully. OrderId: {OrderId}, ReceiptNumber: {ReceiptNumber}, TotalAmount: {TotalAmount}",
                order.Id,
                receiptNumber,
                totalAmount);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Order,
                    Action = OperationalAuditActions.FinalizeSuccess,
                    EntityType = nameof(Order),
                    EntityId = order.Id,
                    OrderId = order.Id,
                    OperationId = request.IdempotencyKey,
                    CorrelationId = request.IdempotencyKey,
                    Severity = OperationalAuditSeverity.Information,
                    Summary = "Order finalized successfully",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["receiptNumber"] = receiptNumber,
                        ["totalAmount"] = totalAmount,
                        ["netCaptured"] = netCaptured
                    }
                },
                cancellationToken);

            // Table release — mark table Available after order is paid (non-fatal).
            if (order.TableId.HasValue)
            {
                try
                {
                    var table = await _dbContext.Set<Table>()
                        .FindAsync(new object[] { order.TableId.Value }, cancellationToken);
                    if (table != null && table.Status == TableStatus.Occupied)
                    {
                        table.Status    = TableStatus.Cleaning; // staff clears before next seating
                        table.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception tableEx)
                {
                    _logger.LogError(tableEx,
                        "Table release failed after successful finalize (non-fatal). OrderId={OrderId}, TableId={TableId}",
                        order.Id, order.TableId);
                }
            }

            // Loyalty point accrual — runs AFTER the main transaction commits.
            // GOVERNANCE: loyalty is a separate concern; a loyalty failure must never roll back a completed sale.
            // Points = floor(TotalAmount * LoyaltyPointsPerDollar). Only when customer is attached and loyalty is enabled.
            if (order.CustomerId.HasValue && businessSettings?.LoyaltyEnabled == true && businessSettings.LoyaltyPointsPerDollar > 0)
            {
                try
                {
                    var points = (int)Math.Floor(totalAmount * businessSettings.LoyaltyPointsPerDollar);
                    if (points > 0)
                    {
                        var earnAccount = await _dbContext.Set<LoyaltyAccount>()
                            .FirstOrDefaultAsync(la => la.CustomerId == order.CustomerId.Value && la.IsActive, cancellationToken);

                        if (earnAccount == null)
                        {
                            earnAccount = new LoyaltyAccount { CustomerId = order.CustomerId.Value };
                            _dbContext.Set<LoyaltyAccount>().Add(earnAccount);
                        }

                        earnAccount.PointBalance         += points;
                        earnAccount.LifetimePointsEarned += points;
                        earnAccount.UpdatedAt             = DateTime.UtcNow;

                        _dbContext.Set<LoyaltyTransaction>().Add(new LoyaltyTransaction
                        {
                            LoyaltyAccountId = earnAccount.Id,
                            Points           = points,
                            TransactionType  = LoyaltyTransactionType.Earn,
                            OrderId          = order.Id,
                            Notes            = $"Earned on order {order.OrderNumber}"
                        });

                        await _dbContext.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Loyalty points accrued. CustomerId={CustomerId}, Points={Points}, OrderId={OrderId}",
                            order.CustomerId.Value, points, order.Id);
                    }
                }
                catch (Exception loyaltyEx)
                {
                    // Non-fatal: log and continue — the sale is already committed.
                    _logger.LogError(loyaltyEx,
                        "Loyalty accrual failed after successful finalize (non-fatal). OrderId={OrderId}",
                        order.Id);
                }
            }

            // SMS/WhatsApp order confirmation — runs AFTER the main transaction and loyalty.
            // GOVERNANCE: notification failure must never affect the completed sale.
            // Only fires when the order has a customer phone number.
            if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                try
                {
                    await _notificationService.SendOrderConfirmationAsync(
                        toPhone:       order.CustomerPhone,
                        orderNumber:   order.OrderNumber,
                        receiptNumber: order.ReceiptNumber,
                        totalAmount:   order.TotalAmount,
                        currency:      businessSettings?.Currency ?? "USD",
                        businessName:  businessSettings?.BusinessName ?? "Tannous POS",
                        cancellationToken: cancellationToken);
                }
                catch (Exception notifEx)
                {
                    // Non-fatal: log and continue — the sale is already committed.
                    _logger.LogError(notifEx,
                        "Order confirmation notification failed after successful finalize (non-fatal). OrderId={OrderId}",
                        order.Id);
                }
            }

            _ = _webhookDispatcher.DispatchAsync(
                WebhookEventType.OrderFinalized,
                new
                {
                    orderId     = order.Id,
                    orderNumber = order.OrderNumber,
                    total       = order.TotalAmount,
                    currency    = businessSettings?.Currency ?? "USD",
                    customerId  = order.CustomerId,
                    orderType   = order.OrderType.ToString()
                },
                branchId: order.BranchId,
                cancellationToken: cancellationToken);

            // Return updated order DTO
            return MapToOrderDto(order);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var affectedTypes = ConcurrencyConflictObservability.FormatAffectedClrTypeNames(ex);
            _logger.LogWarning(
                ex,
                "Money-path concurrency visibility: optimistic concurrency conflict during finalize (order/inventory/shift RowVersion). OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, AffectedEntityTypes={AffectedEntityTypes}",
                request.OrderId,
                request.IdempotencyKey,
                affectedTypes);

            if (!joinsOuterTransaction && ownedTransaction != null)
            {
                try
                {
                    await ownedTransaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error during rollback after concurrency conflict. OrderId: {OrderId}", request.OrderId);
                }
            }

            await _syncConflictRecorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    OperationId = request.IdempotencyKey,
                    EntityType = nameof(Order),
                    EntityId = request.OrderId,
                    ConflictType = SyncConflictTypes.ConcurrencyConflict,
                    Reason = $"DbUpdateConcurrencyException during finalize ({affectedTypes})",
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
                    Summary = $"Concurrency conflict during finalize ({affectedTypes})"
                },
                cancellationToken);

            throw new InvalidOperationException(
                "Order or inventory was modified concurrently. Refresh the order and retry finalize.");
        }
        catch (Exception ex)
        {
            // Rollback transaction on any error to prevent partial state
            _logger.LogError(
                ex,
                "Error during order finalization. Rolling back transaction. OrderId: {OrderId}",
                request.OrderId);

            if (!joinsOuterTransaction && ownedTransaction != null)
            {
                try
                {
                    await ownedTransaction.RollbackAsync(cancellationToken);
                    _logger.LogInformation("Transaction rolled back successfully. OrderId: {OrderId}", request.OrderId);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Error during transaction rollback. OrderId: {OrderId}",
                        request.OrderId);
                    // Re-throw original exception, not rollback exception
                }
            }

            // Re-throw original exception
            throw;
        }
        finally
        {
            if (ownedTransaction != null)
            {
                await ownedTransaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Creates inventory deduction movements for all recipe ingredients in the order.
    /// Aggregates quantities per ingredient to create minimal movement records.
    /// Note: System allows negative stock (no validation blocking). Stock can go below zero.
    /// </summary>
    private async Task CreateInventoryDeductionsAsync(Order order, string idempotencyKey, CancellationToken cancellationToken)
    {
        // Collect unique menu item IDs from order lines
        var menuItemIds = order.OrderLines
            .Select(ol => ol.MenuItemId)
            .Distinct()
            .ToList();

        if (!menuItemIds.Any())
        {
            _logger.LogInformation("No menu items in order. Skipping inventory deduction. OrderId: {OrderId}", order.Id);
            return;
        }

        // Batch load all recipes for menu items in the order (avoid N+1 queries)
        var allRecipes = await _dbContext.Set<Recipe>()
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => menuItemIds.Contains(r.MenuItemId) && r.IsActive)
            .ToListAsync(cancellationToken);

        if (!allRecipes.Any())
        {
            _logger.LogInformation(
                "No active recipes found for menu items in order. Skipping inventory deduction. OrderId: {OrderId}",
                order.Id);
            return;
        }

        // Create a dictionary to map menu item ID to its recipe(s)
        // Note: A menu item can have multiple recipes, but we'll use the first active one
        var menuItemRecipeMap = allRecipes
            .GroupBy(r => r.MenuItemId)
            .ToDictionary(g => g.Key, g => g.First());

        // Aggregate ingredient quantities across all order lines
        // Key: IngredientId, Value: Total quantity to deduct
        var ingredientQuantities = new Dictionary<Guid, decimal>();

        foreach (var orderLine in order.OrderLines)
        {
            if (!menuItemRecipeMap.TryGetValue(orderLine.MenuItemId, out var recipe))
            {
                _logger.LogDebug(
                    "No recipe found for menu item {MenuItemId} in order {OrderId}. Skipping inventory deduction for this item.",
                    orderLine.MenuItemId,
                    order.Id);
                continue;
            }

            // For each recipe line, calculate total quantity needed
            foreach (var recipeLine in recipe.RecipeLines)
            {
                // Quantity per order = recipe quantity per item * order line quantity
                var totalQuantity = recipeLine.QuantityPerItem * orderLine.Quantity;

                if (ingredientQuantities.ContainsKey(recipeLine.IngredientId))
                {
                    ingredientQuantities[recipeLine.IngredientId] += totalQuantity;
                }
                else
                {
                    ingredientQuantities[recipeLine.IngredientId] = totalQuantity;
                }
            }
        }

        if (!ingredientQuantities.Any())
        {
            _logger.LogInformation(
                "No ingredient quantities to deduct. Skipping inventory movements. OrderId: {OrderId}",
                order.Id);
            return;
        }

        // GOVERNANCE / RISK: inventory consistency observability only — negative stock remains allowed; no reservation or retry.
        _logger.LogInformation(
            "Inventory consistency observability: finalize inventory deduction pass starting. OrderId={OrderId}, IdempotencyKey={IdempotencyKey}, DistinctIngredientCount={DistinctIngredientCount}",
            order.Id,
            idempotencyKey,
            ingredientQuantities.Count);

        // Batch load all inventory items for the ingredients (avoid N+1 queries)
        var ingredientIds = ingredientQuantities.Keys.ToList();
        var inventoryItems = await _dbContext.Set<InventoryItem>()
            .Include(ii => ii.Ingredient)
            .Where(ii => ingredientIds.Contains(ii.IngredientId))
            .ToListAsync(cancellationToken);

        var inventoryItemMap = inventoryItems.ToDictionary(ii => ii.IngredientId);

        // Create inventory movements and update stock for each ingredient
        var movementsCreated = 0;
        foreach (var (ingredientId, quantityToDeduct) in ingredientQuantities)
        {
            // Get or create inventory item
            if (!inventoryItemMap.TryGetValue(ingredientId, out var inventoryItem))
            {
                // Create inventory item if it doesn't exist (shouldn't happen in normal flow, but handle gracefully)
                _logger.LogWarning(
                    "Inventory item not found for ingredient {IngredientId}. Creating new inventory item. OrderId: {OrderId}",
                    ingredientId,
                    order.Id);

                var ingredient = await _dbContext.Set<Ingredient>().FindAsync(new object[] { ingredientId }, cancellationToken);
                if (ingredient == null)
                {
                    _logger.LogError(
                        "Ingredient {IngredientId} not found. Skipping inventory deduction. OrderId: {OrderId}",
                        ingredientId,
                        order.Id);
                    continue;
                }

                inventoryItem = new InventoryItem
                {
                    IngredientId = ingredientId,
                    CurrentStock = 0,
                    MinimumStock = 0,
                    MaximumStock = 0,
                    AverageCost = ingredient.CostPerUnit,
                    Unit = ingredient.Unit,
                    LastUpdated = DateTime.UtcNow
                };
                _dbContext.Set<InventoryItem>().Add(inventoryItem);
                inventoryItemMap[ingredientId] = inventoryItem;
            }

            // Update inventory stock (deduct quantity)
            // Note: System allows negative stock. No validation blocking.
            inventoryItem.CurrentStock -= quantityToDeduct;
            inventoryItem.LastUpdated = DateTime.UtcNow;

            if (inventoryItem.CurrentStock < 0)
            {
                _logger.LogWarning(
                    "Inventory consistency observability: negative stock after finalize sale deduction (allowed by current domain rules). OrderId={OrderId}, InventoryItemId={InventoryItemId}, IngredientId={IngredientId}, IdempotencyKey={IdempotencyKey}, CurrentStock={CurrentStock}",
                    order.Id,
                    inventoryItem.Id,
                    ingredientId,
                    idempotencyKey,
                    inventoryItem.CurrentStock);

                await _syncConflictRecorder.RecordAsync(
                    new SyncConflictRecordRequest
                    {
                        OperationId = idempotencyKey,
                        EntityType = nameof(InventoryItem),
                        EntityId = inventoryItem.Id,
                        ConflictType = SyncConflictTypes.InventoryDriftRisk,
                        Reason =
                            $"Negative stock after finalize deduction (OrderId={order.Id}, IngredientId={ingredientId}, CurrentStock={inventoryItem.CurrentStock})",
                        CorrelationId = idempotencyKey
                    },
                    cancellationToken);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Inventory,
                        Action = OperationalAuditActions.NegativeStockDetected,
                        EntityType = nameof(InventoryItem),
                        EntityId = inventoryItem.Id,
                        OrderId = order.Id,
                        OperationId = idempotencyKey,
                        CorrelationId = idempotencyKey,
                        Severity = OperationalAuditSeverity.Warning,
                        Summary = "Negative stock detected after finalize deduction",
                        Metadata = new Dictionary<string, object?>
                        {
                            ["ingredientId"] = ingredientId,
                            ["currentStock"] = inventoryItem.CurrentStock
                        }
                    },
                    cancellationToken);
            }

            // Create inventory movement record
            var movement = new InventoryMovement
            {
                IngredientId = ingredientId,
                InventoryItemId = inventoryItem.Id,
                MovementType = InventoryMovementType.Sale,
                Quantity = -quantityToDeduct, // Negative quantity for deduction
                UnitCost = inventoryItem.AverageCost,
                TotalCost = -quantityToDeduct * inventoryItem.AverageCost,
                Reference = $"Order-{order.OrderNumber}",
                Notes = $"Sale deduction for order {order.OrderNumber}",
                MovementDate = DateTime.UtcNow
            };

            await _inventoryRepository.AddMovementAsync(movement);
            movementsCreated++;

            _logger.LogDebug(
                "Created inventory deduction. IngredientId: {IngredientId}, Quantity: {Quantity}, NewStock: {NewStock}, OrderId: {OrderId}",
                ingredientId,
                quantityToDeduct,
                inventoryItem.CurrentStock,
                order.Id);
        }

        _logger.LogInformation(
            "Inventory consistency observability: persisted inventory movements for finalize. Count={Count}, OrderId={OrderId}, IdempotencyKey={IdempotencyKey}",
            movementsCreated,
            order.Id,
            idempotencyKey);

        await _operationalAuditRecorder.RecordAsync(
            new OperationalAuditRecordRequest
            {
                Category = OperationalAuditCategories.Inventory,
                Action = OperationalAuditActions.InventoryDeductionPass,
                EntityType = nameof(Order),
                EntityId = order.Id,
                OrderId = order.Id,
                OperationId = idempotencyKey,
                CorrelationId = idempotencyKey,
                Severity = OperationalAuditSeverity.Information,
                Summary = "Inventory deduction pass completed for finalize",
                Metadata = new Dictionary<string, object?>
                {
                    ["movementCount"] = movementsCreated
                }
            },
            cancellationToken);
    }

    private static OrderDto MapToOrderDto(Order order)
    {
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
            StampDutyAmount = order.StampDutyAmount,
            TotalAmount = order.TotalAmount,
            ReceiptNumber = order.ReceiptNumber,
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
