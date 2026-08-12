using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Security.Claims;
using System.Text.Json;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Application.DTOs.Sync;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Application.Orders.Commands.CreateOrder;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Orders.Commands.FinalizeOrder;
using Tannous.Pos.Application.Shifts.Commands.CashDrop;
using Tannous.Pos.Application.Inventory.Commands.AdjustInventory;
using Tannous.Pos.Application.Inventory.Commands.RecordWastage;
using Tannous.Pos.Application.Sync.Queries.GetSyncPullData;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class SyncController : ControllerBase
{
    private const int MaxPushOperationsPerRequest = 200;
    private readonly ISyncService _syncService;
    private readonly IMediator _mediator;
    private readonly IDurableSyncReplayCoordinator _replayCoordinator;
    private readonly ISyncPushOperationExecutionScope _pushExecutionScope;
    private readonly ISyncConflictRecorder _syncConflictRecorder;
    private readonly IOperationalAuditRecorder _operationalAuditRecorder;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ISyncService syncService,
        IMediator mediator,
        IDurableSyncReplayCoordinator replayCoordinator,
        ISyncPushOperationExecutionScope pushExecutionScope,
        ISyncConflictRecorder syncConflictRecorder,
        IOperationalAuditRecorder operationalAuditRecorder,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _mediator = mediator;
        _replayCoordinator = replayCoordinator;
        _pushExecutionScope = pushExecutionScope;
        _syncConflictRecorder = syncConflictRecorder;
        _operationalAuditRecorder = operationalAuditRecorder;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SyncData()
    {
        var result = await _syncService.SyncDataAsync();
        if (result)
        {
            return Ok(new { message = "Data synchronized successfully" });
        }
        return BadRequest(new { message = "Failed to synchronize data" });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        var lastSyncTime = await _syncService.GetLastSyncTimeAsync();
        var isSyncRequired = await _syncService.IsSyncRequiredAsync();

        return Ok(new
        {
            lastSyncTime,
            isSyncRequired,
            nextSyncRecommended = lastSyncTime.AddMinutes(30)
        });
    }

    [HttpGet("pull")]
    public async Task<ActionResult<PullResponseDto>> PullData([FromQuery] string? since = null, [FromQuery] int limit = 500, [FromQuery] string? token = null)
    {
        var cancellationToken = HttpContext.RequestAborted;
        // Bound page size to reduce memory pressure from accidental huge pulls.
        limit = Math.Clamp(limit, 1, 1000);

        var cursor = since ?? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffZ|v0");
        var sinceDate = ParseCursor(cursor);
        var offset = ParsePaginationToken(token);

        var result = await _mediator.Send(new GetSyncPullDataQuery
        {
            SinceDate = sinceDate,
            Limit     = limit,
            Offset    = offset
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("push")]
    public async Task<ActionResult<PushResponseDto>> PushData([FromBody] PushRequestDto request)
    {
        var cancellationToken = HttpContext.RequestAborted;
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Sync push received: operationCount={Count}, deviceIdLength={DeviceIdLen}",
            request.Operations.Count,
            request.DeviceId?.Length ?? 0);

        var operations = request.Operations;
        if (operations.Count > MaxPushOperationsPerRequest)
        {
            _logger.LogWarning(
                "Sync push operation count {Count} exceeds cap {Cap}; truncating to bounded batch.",
                operations.Count,
                MaxPushOperationsPerRequest);
            operations = operations.Take(MaxPushOperationsPerRequest).ToList();
        }

        var response = new PushResponseDto
        {
            Results = new List<OpResultDto>(),
            NewCursor = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ|v1")
        };
        if (request.Operations.Count > operations.Count)
            response.Warnings.Add($"Only first {operations.Count} operations were processed in this request.");

        var seenOperationIds = new HashSet<string>(StringComparer.Ordinal);
        var batchTelemetry = new SyncPushBatchTelemetry();

        // GOVERNANCE / RISK: Partial batch / mixed success — individual operations can fail while others succeed; reconciliation or manual review may still be needed against device expectations.
        // GOVERNANCE / RISK: Durable SyncOperationReceipt (deviceId+operationId) applies to all known push types in DurableSyncReplayProtectedTypes (money, inventory, OpenShift, CreateCustomer). Unknown operation types remain without durable replay.
        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = "Operation not supported"
            };
            var unexpectedException = false;

            if (string.IsNullOrWhiteSpace(operation.OpId))
            {
                _logger.LogWarning(
                    "Sync replay visibility: operation missing operationId (replay/idempotency correlation impaired). OperationType={OperationType}",
                    operation.Type);
            }
            else if (!seenOperationIds.Add(operation.OpId))
            {
                _logger.LogWarning(
                    "Sync replay visibility: duplicate operationId within same batch (client retry or bug). OperationId={OperationId}, OperationType={OperationType}, ReplayRisk=money-or-inventory-if-payload-differs. Later duplicates still run through processors; durable replay may short-circuit after first completes.",
                    operation.OpId,
                    operation.Type);
            }

            try
            {
                switch (operation.Type)
                {
                    case "CreateCustomer":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessCreateCustomer(request.DeviceId ?? string.Empty, operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "CreateOrder":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessCreateOrder(operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "FinalizeOrder":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessFinalizeOrder(operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "OpenShift":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessOpenShift(request.DeviceId ?? string.Empty, operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "CashDrop":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessCashDrop(operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "RecordWastage":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessRecordWastage(operation, cancellationToken),
                            cancellationToken);
                        break;
                    case "AdjustInventory":
                        result = await _replayCoordinator.ExecuteAsync(
                            request.DeviceId,
                            operation.OpId,
                            operation.Type,
                            () => ProcessAdjustInventory(operation, cancellationToken),
                            cancellationToken);
                        break;
                    default:
                        result.Message = $"Unknown operation type: {operation.Type}";
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                unexpectedException = true;
                result.Message = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Sync batch observability: unexpected exception during operation (batch continues). OperationType={OperationType}, OperationId={OperationId}",
                    operation.Type,
                    operation.OpId);
            }

            var replayShortCircuited = _pushExecutionScope.ConsumeReplayShortCircuited();
            var classification = SyncOperationOutcomeClassifier.Classify(
                operation,
                result,
                replayShortCircuited,
                unexpectedException);
            batchTelemetry.Record(classification, result, operation.Type);

            _logger.LogInformation(
                "Sync batch observability: operation classified. OperationType={OperationType}, OperationId={OperationId}, Classification={Classification}, Success={Success}, Conflict={Conflict}, ReplayShortCircuited={ReplayShortCircuited}",
                operation.Type,
                operation.OpId,
                classification,
                result.Success,
                result.Conflict,
                replayShortCircuited);

            response.Results.Add(result);
        }

        var okCount = batchTelemetry.SuccessCount;
        var failCount = batchTelemetry.FailureCount;
        var conflictCount = batchTelemetry.ConflictCount;
        var partialBatchRisk = batchTelemetry.IsPartialBatchRisk;

        _logger.LogInformation(
            "Sync push completed: success={Ok}, failed={Fail}, conflict={Conflict}, batchSize={BatchSize}",
            okCount,
            failCount,
            conflictCount,
            batchTelemetry.BatchSize);

        _logger.LogInformation(
            "Sync batch observability: push batch summary. BatchSize={BatchSize}, SuccessCount={SuccessCount}, FailureCount={FailureCount}, ConflictCount={ConflictCount}, ReplayShortCircuitCount={ReplayShortCircuitCount}, PlaceholderCount={PlaceholderCount}, ValidationFailureCount={ValidationFailureCount}, RetryableFailureCount={RetryableFailureCount}, NonRetryableFailureCount={NonRetryableFailureCount}, PartialBatchRisk={PartialBatchRisk}",
            batchTelemetry.BatchSize,
            batchTelemetry.SuccessCount,
            batchTelemetry.FailureCount,
            batchTelemetry.ConflictCount,
            batchTelemetry.ReplayShortCircuitCount,
            batchTelemetry.PlaceholderCount,
            batchTelemetry.ValidationFailureCount,
            batchTelemetry.RetryableFailureCount,
            batchTelemetry.NonRetryableFailureCount,
            partialBatchRisk);

        if (partialBatchRisk)
        {
            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Reconciliation,
                    Action = OperationalAuditActions.MixedBatchOutcomes,
                    EntityType = "SyncPushBatch",
                    DeviceId = request.DeviceId,
                    Severity = OperationalAuditSeverity.Warning,
                    Summary = "Sync push batch had mixed outcomes (partial batch risk)",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["successCount"] = okCount,
                        ["failureCount"] = failCount,
                        ["conflictCount"] = conflictCount,
                        ["batchSize"] = batchTelemetry.BatchSize
                    }
                },
                cancellationToken);

            _logger.LogWarning(
                "Sync batch observability: partial batch classification (mixed outcomes; reconciliation may be required). Success={Ok}, Failed={Fail}, Conflict={Conflict}, BatchSize={BatchSize}, ReplayShortCircuitCount={ReplayShortCircuitCount}, PlaceholderCount={PlaceholderCount}",
                okCount,
                failCount,
                conflictCount,
                batchTelemetry.BatchSize,
                batchTelemetry.ReplayShortCircuitCount,
                batchTelemetry.PlaceholderCount);

            _logger.LogWarning(
                "Sync replay visibility: partial application / mixed batch outcomes (reconciliation may be required). Success={Ok}, Failed={Fail}, Conflict={Conflict}, BatchSize={BatchSize}",
                okCount,
                failCount,
                conflictCount,
                batchTelemetry.BatchSize);
        }

        if (batchTelemetry.HasReplayMixedWithFailureOrConflict)
        {
            _logger.LogWarning(
                "Sync reconciliation visibility: replay mixed with failed operations. DeviceId={DeviceId}, ReplayCount={ReplayCount}, FailureCount={FailureCount}, ConflictCount={ConflictCount}",
                request.DeviceId,
                batchTelemetry.ReplayShortCircuitCount,
                batchTelemetry.FailureCount,
                batchTelemetry.ConflictCount);
        }

        if (batchTelemetry.HasMixedPlaceholderAndReplayVisibility)
        {
            _logger.LogWarning(
                "Sync reconciliation visibility: mixed placeholder and replay short-circuit in batch. DeviceId={DeviceId}, CustomerShiftReplayCount={CustomerShiftReplayCount}, PlaceholderCount={PlaceholderCount}, FailureCount={FailureCount}, ConflictCount={ConflictCount}",
                request.DeviceId,
                batchTelemetry.CustomerShiftReplayShortCircuitCount,
                batchTelemetry.PlaceholderCount,
                batchTelemetry.FailureCount,
                batchTelemetry.ConflictCount);
        }

        if (batchTelemetry.HasMixedInventoryAndReplayVisibility)
        {
            _logger.LogWarning(
                "Sync reconciliation visibility: mixed inventory replay and failed operations in batch. DeviceId={DeviceId}, InventoryReplayCount={InventoryReplayCount}, FailureCount={FailureCount}, ConflictCount={ConflictCount}",
                request.DeviceId,
                batchTelemetry.InventoryReplayShortCircuitCount,
                batchTelemetry.FailureCount,
                batchTelemetry.ConflictCount);
        }

        await RecordBatchReconciliationConflictsAsync(request.DeviceId, batchTelemetry, cancellationToken);

        return Ok(response);
    }

    private async Task RecordBatchReconciliationConflictsAsync(
        string? deviceId,
        SyncPushBatchTelemetry batchTelemetry,
        CancellationToken cancellationToken)
    {
        if (batchTelemetry.HasMixedInventoryAndReplayVisibility)
        {
            await _syncConflictRecorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    DeviceId = deviceId,
                    EntityType = "SyncPushBatch",
                    ConflictType = SyncConflictTypes.InventoryDriftRisk,
                    Reason =
                        "Partial batch: mixed inventory replay short-circuit with failed or conflict operations (reconciliation visibility)"
                },
                cancellationToken);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Reconciliation,
                    Action = OperationalAuditActions.PartialBatchReconciliation,
                    EntityType = "SyncPushBatch",
                    DeviceId = deviceId,
                    Severity = OperationalAuditSeverity.Warning,
                    Summary = "Partial batch reconciliation: mixed inventory replay with failures"
                },
                cancellationToken);
        }

        if (batchTelemetry.HasPartialBatchInventoryReconciliation)
        {
            await _syncConflictRecorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    DeviceId = deviceId,
                    EntityType = "SyncPushBatch",
                    ConflictType = SyncConflictTypes.InventoryDriftRisk,
                    Reason =
                        $"Partial batch: inventory operation failures in mixed batch (InventoryFailureCount={batchTelemetry.InventoryOperationFailureCount})"
                },
                cancellationToken);

            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Reconciliation,
                    Action = OperationalAuditActions.PartialBatchReconciliation,
                    EntityType = "SyncPushBatch",
                    DeviceId = deviceId,
                    Severity = OperationalAuditSeverity.Warning,
                    Summary = "Partial batch reconciliation: inventory operation failures",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["inventoryFailureCount"] = batchTelemetry.InventoryOperationFailureCount
                    }
                },
                cancellationToken);
        }
    }

    private static DateTime ParseCursor(string cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return DateTime.UtcNow.AddDays(-1);

        // AdjustToUniversal is required: default DateTime.TryParse converts "...Z" strings to
        // Kind=Local, and Npgsql rejects non-UTC DateTimes against 'timestamp with time zone'
        // (ArgumentException -> mapped to HTTP 400 by GlobalExceptionHandler).
        var parts = cursor.Split('|');
        if (parts.Length > 0 && DateTime.TryParse(
                parts[0],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var date))
            return date;

        return DateTime.UtcNow.AddDays(-1);
    }

    private static int ParsePaginationToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || !int.TryParse(token, out var offset))
            return 0;
        return offset;
    }

    private async Task<OpResultDto> ProcessCreateCustomer(
        string deviceId,
        OutboxOperationDto operation,
        CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: Placeholder success — processor has no EF customer persistence; durable SyncOperationReceipt at push wrapper suppresses duplicate replay.
        // Replay sensitivity classification: placeholder-only (receipt-protected at coordinator).
        // Operational impact: mobile may assume customer exists server-side; ServerId echoes OpId until a real command exists.
        // Direction: route through Application command + outbox idempotency store keyed by operationId (see push handler).
        // Simplified implementation - would need proper validation and mapping
        _logger.LogWarning(
            "Sync replay visibility: placeholder-only processor (CreateCustomer); durable SyncOperationReceipt at push wrapper. OperationType={OperationType}, OpId={OpId}, ReplayClass=placeholder-only",
            operation.Type,
            operation.OpId);

        await _operationalAuditRecorder.RecordAsync(
            new OperationalAuditRecordRequest
            {
                Category = OperationalAuditCategories.Replay,
                Action = OperationalAuditActions.PlaceholderOperationExecuted,
                EntityType = "SyncOperation",
                DeviceId = deviceId,
                OperationId = operation.OpId,
                CorrelationId = operation.OpId,
                Severity = OperationalAuditSeverity.Information,
                Summary = "Placeholder CreateCustomer processor executed",
                DedupeByDeviceOperationAndAction = true,
                Metadata = new Dictionary<string, object?> { ["operationType"] = operation.Type }
            },
            cancellationToken);

        return new OpResultDto
        {
            OpId = operation.OpId,
            Success = true,
            ServerId = operation.OpId,
            Message = "Customer created successfully"
        };
    }

    private async Task<OpResultDto> ProcessCreateOrder(OutboxOperationDto operation, CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: replay / idempotency — durable SyncOperationReceipt dedupe remains in push wrapper; this method executes real Application CreateOrder flow.
        // Replay sensitivity classification: money-affecting.
        // Direction: keep SyncController transport-only by mapping payload and delegating to MediatR.
        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = "Invalid user token"
            };
        }

        if (!TryBuildCreateOrderDto(operation.Payload, out var createOrderDto, out var shiftId, out var payloadError))
        {
            _logger.LogWarning(
                "Sync CreateOrder payload validation failed. OperationId={OperationId}, Error={Error}",
                operation.OpId,
                payloadError);
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = payloadError
            };
        }

        var command = new CreateOrderCommand
        {
            Order = createOrderDto!,
            UserId = userId.Value,
            ShiftId = shiftId
        };

        _logger.LogInformation(
            "Sync CreateOrder dispatching MediatR command. replay visibility classification=money-affecting. OperationId={OperationId}, UserId={UserId}, ShiftId={ShiftId}, LineCount={LineCount}",
            operation.OpId,
            userId,
            shiftId,
            createOrderDto!.OrderLines.Count);

        var order = await _mediator.Send(command, cancellationToken);
        return new OpResultDto
        {
            OpId = operation.OpId,
            Success = true,
            ServerId = order.Id.ToString(),
            Message = "Order created successfully"
        };
    }

    private async Task<OpResultDto> ProcessFinalizeOrder(OutboxOperationDto operation, CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: replay / idempotency — durable SyncOperationReceipt short-circuits retries around this real finalize command execution.
        // Replay sensitivity classification: money-affecting.
        // Direction: payload mapping only; business rules/transactions/concurrency belong to FinalizeOrderCommandHandler.
        if (!TryBuildFinalizeCommand(operation, out var command, out var payloadError))
        {
            _logger.LogWarning(
                "Sync FinalizeOrder payload validation failed. OperationId={OperationId}, Error={Error}",
                operation.OpId,
                payloadError);
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = payloadError
            };
        }

        _logger.LogInformation(
            "Sync FinalizeOrder dispatching MediatR command. replay visibility classification=money-affecting. OperationId={OperationId}, OrderId={OrderId}, PaymentCount={PaymentCount}",
            operation.OpId,
            command!.OrderId,
            command.Payments.Count);

        try
        {
            var order = await _mediator.Send(command, cancellationToken);
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = true,
                ServerId = order.Id.ToString(),
                Message = "Order finalized successfully"
            };
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified concurrently", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                ex,
                "Sync FinalizeOrder concurrency conflict. OperationId={OperationId}, OrderId={OrderId}",
                operation.OpId,
                command.OrderId);
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Conflict = true,
                Message = "Order or inventory was modified concurrently. Refresh and retry."
            };
        }
    }

    private async Task<OpResultDto> ProcessOpenShift(
        string deviceId,
        OutboxOperationDto operation,
        CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: Placeholder success — shift/cash mutations not applied server-side via this path; durable SyncOperationReceipt at push wrapper suppresses duplicate replay.
        // Replay sensitivity classification: placeholder-only (receipt-protected at coordinator).
        // Direction: dispatch OpenShiftCommand with operationId idempotency.
        _logger.LogWarning(
            "Sync replay visibility: placeholder-only processor (OpenShift); durable SyncOperationReceipt at push wrapper. OperationType={OperationType}, OpId={OpId}, ReplayClass=placeholder-only",
            operation.Type,
            operation.OpId);

        await _operationalAuditRecorder.RecordAsync(
            new OperationalAuditRecordRequest
            {
                Category = OperationalAuditCategories.Replay,
                Action = OperationalAuditActions.PlaceholderOperationExecuted,
                EntityType = "SyncOperation",
                DeviceId = deviceId,
                OperationId = operation.OpId,
                CorrelationId = operation.OpId,
                Severity = OperationalAuditSeverity.Information,
                Summary = "Placeholder OpenShift processor executed",
                DedupeByDeviceOperationAndAction = true,
                Metadata = new Dictionary<string, object?> { ["operationType"] = operation.Type }
            },
            cancellationToken);

        return new OpResultDto
        {
            OpId = operation.OpId,
            Success = true,
            ServerId = operation.OpId,
            Message = "Shift opened successfully"
        };
    }

    private async Task<OpResultDto> ProcessCashDrop(OutboxOperationDto operation, CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: replay / idempotency — durable SyncOperationReceipt dedupe remains in push wrapper; this method executes real Application cash-drop flow.
        // Replay sensitivity classification: money-affecting.
        // Direction: payload mapping only; domain validation/transactions remain in command handler.
        if (!TryBuildCashDropCommand(operation.Payload, operation.OpId, out var command, out var payloadError))
        {
            _logger.LogWarning(
                "Sync CashDrop payload validation failed. OperationId={OperationId}, Error={Error}",
                operation.OpId,
                payloadError);
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = payloadError
            };
        }

        _logger.LogInformation(
            "Sync CashDrop dispatching MediatR command. replay visibility classification=money-affecting. OperationId={OperationId}, ShiftId={ShiftId}, Amount={Amount}",
            operation.OpId,
            command!.ShiftId,
            command.Amount);

        var cashDrop = await _mediator.Send(command, cancellationToken);
        return new OpResultDto
        {
            OpId = operation.OpId,
            Success = true,
            ServerId = cashDrop.Id.ToString(),
            Message = "Cash drop recorded successfully"
        };
    }

    private Guid? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }

    private bool TryBuildCreateOrderDto(
        Dictionary<string, object?> payload,
        out CreateOrderDto? dto,
        out Guid? shiftId,
        out string error)
    {
        dto = null;
        shiftId = null;
        error = string.Empty;

        if (payload.TryGetValue("shiftId", out var shiftIdObj) &&
            Guid.TryParse(shiftIdObj?.ToString(), out var parsedShiftId))
        {
            shiftId = parsedShiftId;
        }

        var orderType = OrderType.DineIn;
        if (payload.TryGetValue("orderType", out var orderTypeObj))
        {
            var orderTypeText = orderTypeObj?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(orderTypeText) &&
                !Enum.TryParse<OrderType>(orderTypeText, true, out orderType))
            {
                error = "Invalid orderType";
                return false;
            }
        }

        var customerId = default(Guid?);
        if (payload.TryGetValue("customerId", out var customerIdObj) &&
            !string.IsNullOrWhiteSpace(customerIdObj?.ToString()))
        {
            if (!Guid.TryParse(customerIdObj!.ToString(), out var parsedCustomerId))
            {
                error = "Invalid customerId format";
                return false;
            }
            customerId = parsedCustomerId;
        }

        if (!TryGetJsonElement(payload, "orderLines", out var orderLinesElement) &&
            !TryGetJsonElement(payload, "lines", out orderLinesElement))
        {
            error = "Missing required lines/orderLines payload";
            return false;
        }

        if (orderLinesElement.ValueKind != JsonValueKind.Array || orderLinesElement.GetArrayLength() == 0)
        {
            error = "Order lines payload must be a non-empty array";
            return false;
        }

        var lineDtos = new List<Tannous.Pos.Application.DTOs.Orders.OrderLineDto>();
        foreach (var line in orderLinesElement.EnumerateArray())
        {
            if (!TryReadGuid(line, "menuItemId", out var menuItemId))
            {
                error = "Each line requires a valid menuItemId";
                return false;
            }

            if (!TryReadDecimal(line, "quantity", out var quantity) || quantity <= 0)
            {
                error = "Each line requires quantity > 0";
                return false;
            }

            if (!TryReadDecimal(line, "unitPrice", out var unitPrice) || unitPrice <= 0)
            {
                error = "Each line requires unitPrice > 0";
                return false;
            }

            lineDtos.Add(new Tannous.Pos.Application.DTOs.Orders.OrderLineDto
            {
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }

        dto = new CreateOrderDto
        {
            OrderType = orderType,
            CustomerId = customerId,
            Notes = payload.TryGetValue("notes", out var notesObj) ? notesObj?.ToString() : null,
            OrderLines = lineDtos
        };
        return true;
    }

    private bool TryBuildFinalizeCommand(OutboxOperationDto operation, out FinalizeOrderCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (!operation.Payload.TryGetValue("orderId", out var orderIdObj) ||
            !Guid.TryParse(orderIdObj?.ToString(), out var orderId))
        {
            error = "Missing required orderId in FinalizeOrder payload";
            return false;
        }

        if (!TryGetJsonElement(operation.Payload, "payments", out var paymentsElement) ||
            paymentsElement.ValueKind != JsonValueKind.Array ||
            paymentsElement.GetArrayLength() == 0)
        {
            error = "Missing required payments array";
            return false;
        }

        var payments = new List<Tannous.Pos.Application.Orders.Commands.FinalizeOrder.PaymentDto>();
        foreach (var payment in paymentsElement.EnumerateArray())
        {
            var paymentMethod = TryReadString(payment, "paymentMethod")
                                ?? TryReadString(payment, "method");
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                error = "Each payment requires paymentMethod";
                return false;
            }

            if (!TryReadDecimal(payment, "amount", out var amount) || amount <= 0)
            {
                error = "Each payment requires amount > 0";
                return false;
            }

            payments.Add(new Tannous.Pos.Application.Orders.Commands.FinalizeOrder.PaymentDto
            {
                PaymentMethod = paymentMethod,
                Amount = amount,
                TransactionId = TryReadString(payment, "transactionId"),
                Notes = TryReadString(payment, "notes"),
                TenderedCurrency = TryReadString(payment, "tenderedCurrency") ?? "USD"
            });
        }

        command = new FinalizeOrderCommand
        {
            OrderId = orderId,
            Payments = payments,
            IdempotencyKey = operation.OpId,
            ChangeCurrency = TryGetJsonElement(operation.Payload, "changeCurrency", out var ccEl) &&
                             ccEl.ValueKind == JsonValueKind.String
                ? (ccEl.GetString() ?? "USD")
                : "USD"
        };
        return true;
    }

    private bool TryBuildCashDropCommand(
        Dictionary<string, object?> payload,
        string operationId,
        out CashDropCommand? command,
        out string error)
    {
        command = null;
        error = string.Empty;

        if (!TryReadPayloadDecimal(payload, "amount", out var amount) || amount <= 0)
        {
            error = "Missing required amount > 0 for CashDrop";
            return false;
        }

        if (!payload.TryGetValue("shiftId", out var shiftIdObj) ||
            !Guid.TryParse(shiftIdObj?.ToString(), out var shiftId))
        {
            error = "Missing required shiftId in CashDrop payload";
            return false;
        }

        var note = payload.TryGetValue("note", out var noteObj) ? noteObj?.ToString() : null;
        if (string.IsNullOrWhiteSpace(note) && payload.TryGetValue("reason", out var reasonObj))
            note = reasonObj?.ToString();

        command = new CashDropCommand
        {
            ShiftId = shiftId,
            Amount = amount,
            Note = note,
            IdempotencyKey = operationId
        };
        return true;
    }

    private static bool TryReadPayloadDecimal(Dictionary<string, object?> payload, string key, out decimal value)
    {
        value = 0m;
        return payload.TryGetValue(key, out var obj) &&
               decimal.TryParse(obj?.ToString(), out value);
    }

    private static bool TryGetJsonElement(Dictionary<string, object?> payload, string key, out JsonElement element)
    {
        element = default;
        if (!payload.TryGetValue(key, out var obj) || obj == null)
            return false;

        var raw = obj.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            element = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadGuid(JsonElement element, string property, out Guid value)
    {
        value = Guid.Empty;
        if (!element.TryGetProperty(property, out var prop))
            return false;
        return Guid.TryParse(prop.ToString(), out value);
    }

    private static bool TryReadDecimal(JsonElement element, string property, out decimal value)
    {
        value = 0m;
        if (!element.TryGetProperty(property, out var prop))
            return false;
        return decimal.TryParse(prop.ToString(), out value);
    }

    private static string? TryReadString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop))
            return null;
        var value = prop.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task<OpResultDto> ProcessRecordWastage(OutboxOperationDto operation, CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: Inventory-affecting — durable SyncOperationReceipt + Serializable coordinator dedupe same deviceId+operationId (replay safe; same EF transaction as receipt persistence).
        // GOVERNANCE / RISK: replay / idempotency — repeated OpId returns first recorded OpResult without mutating stock/movements twice when coordinator short-circuits.
        // Replay sensitivity classification: inventory-affecting.
        // Direction: align payload validation with batch duplicate operationId warnings in push handler.
        _logger.LogInformation(
            "Sync ProcessRecordWastage: inventory mutation. replay visibility classification=inventory-affecting. OperationType={OperationType}, OpId={OpId}",
            operation.Type,
            operation.OpId);
        try
        {
            if (!operation.Payload.TryGetValue("ingredientId", out var ingredientIdObj) ||
                !operation.Payload.TryGetValue("quantity", out var quantityObj) ||
                !operation.Payload.TryGetValue("reason", out var reasonObj))
            {
                return new OpResultDto
                {
                    OpId = operation.OpId,
                    Success = false,
                    Message = "Missing required fields: ingredientId, quantity, reason"
                };
            }

            if (!Guid.TryParse(ingredientIdObj?.ToString(), out var ingredientId) ||
                !decimal.TryParse(quantityObj?.ToString(), out var quantity))
            {
                return new OpResultDto
                {
                    OpId = operation.OpId,
                    Success = false,
                    Message = "Invalid ingredientId or quantity format"
                };
            }

            var reason = reasonObj?.ToString() ?? "Unknown";

            return await _mediator.Send(new RecordWastageCommand
            {
                OpId         = operation.OpId,
                IngredientId = ingredientId,
                Quantity     = quantity,
                Reason       = reason
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = $"Error recording wastage: {ex.Message}"
            };
        }
    }

    private async Task<OpResultDto> ProcessAdjustInventory(OutboxOperationDto operation, CancellationToken cancellationToken)
    {
        // GOVERNANCE / RISK: Inventory-affecting — durable SyncOperationReceipt + Serializable coordinator dedupe same deviceId+operationId (replay safe; same EF transaction as receipt persistence).
        // GOVERNANCE / RISK: replay / idempotency — repeated OpId returns first recorded OpResult without mutating stock/movements twice when coordinator short-circuits.
        // Replay sensitivity classification: inventory-affecting.
        // Direction: keep payload validation aligned with batch duplicate operationId warnings in push handler.
        _logger.LogInformation(
            "Sync ProcessAdjustInventory: inventory mutation. replay visibility classification=inventory-affecting. OperationType={OperationType}, OpId={OpId}",
            operation.Type,
            operation.OpId);
        try
        {
            if (!operation.Payload.TryGetValue("ingredientId", out var ingredientIdObj) ||
                !operation.Payload.TryGetValue("quantity", out var quantityObj) ||
                !operation.Payload.TryGetValue("reason", out var reasonObj))
            {
                return new OpResultDto
                {
                    OpId = operation.OpId,
                    Success = false,
                    Message = "Missing required fields: ingredientId, quantity, reason"
                };
            }

            if (!Guid.TryParse(ingredientIdObj?.ToString(), out var ingredientId) ||
                !decimal.TryParse(quantityObj?.ToString(), out var quantity))
            {
                return new OpResultDto
                {
                    OpId = operation.OpId,
                    Success = false,
                    Message = "Invalid ingredientId or quantity format"
                };
            }

            var reason = reasonObj?.ToString() ?? "Unknown";

            return await _mediator.Send(new AdjustInventoryCommand
            {
                OpId         = operation.OpId,
                IngredientId = ingredientId,
                Quantity     = quantity,
                Reason       = reason
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new OpResultDto
            {
                OpId = operation.OpId,
                Success = false,
                Message = $"Error adjusting inventory: {ex.Message}"
            };
        }
    }
}
