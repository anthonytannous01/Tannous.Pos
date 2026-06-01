using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.DTOs.Sync;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class DurableSyncReplayCoordinator : IDurableSyncReplayCoordinator
{
    private readonly PosDbContext _db;
    private readonly ISyncPushOperationExecutionScope _executionScope;
    private readonly ISyncConflictRecorder _syncConflictRecorder;
    private readonly IOperationalAuditRecorder _operationalAuditRecorder;
    private readonly ILogger<DurableSyncReplayCoordinator> _logger;

    public DurableSyncReplayCoordinator(
        PosDbContext db,
        ISyncPushOperationExecutionScope executionScope,
        ISyncConflictRecorder syncConflictRecorder,
        IOperationalAuditRecorder operationalAuditRecorder,
        ILogger<DurableSyncReplayCoordinator> logger)
    {
        _db = db;
        _executionScope = executionScope;
        _syncConflictRecorder = syncConflictRecorder;
        _operationalAuditRecorder = operationalAuditRecorder;
        _logger = logger;
    }

    public async Task<OpResultDto> ExecuteAsync(
        string? deviceId,
        string opId,
        string operationType,
        Func<Task<OpResultDto>> operation,
        CancellationToken cancellationToken = default)
    {
        if (!DurableSyncReplayProtectedTypes.IsProtected(operationType) ||
            string.IsNullOrWhiteSpace(deviceId) ||
            string.IsNullOrWhiteSpace(opId))
        {
            if (DurableSyncReplayProtectedTypes.IsProtected(operationType) &&
                (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(opId)))
            {
                _logger.LogWarning(
                    "Sync durable replay disabled for protected operation (missing DeviceId or operationId). OperationType={OperationType}, HasDeviceId={HasDeviceId}, HasOperationId={HasOperationId}",
                    operationType,
                    !string.IsNullOrWhiteSpace(deviceId),
                    !string.IsNullOrWhiteSpace(opId));
            }

            return await operation();
        }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var existing = await _db.SyncOperationReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.DeviceId == deviceId && r.OperationId == opId,
                cancellationToken);

        if (existing != null)
        {
            if (!string.Equals(existing.OperationType, operationType, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Sync durable replay: operationId reused with different OperationType (rejecting). DeviceId={DeviceId}, OperationId={OperationId}, StoredType={StoredType}, RequestedType={RequestedType}",
                    deviceId,
                    opId,
                    existing.OperationType,
                    operationType);

                if (DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType) ||
                    DurableSyncReplayProtectedTypes.IsInventoryProtected(existing.OperationType))
                {
                    _logger.LogWarning(
                        "Inventory sync durable replay visibility: replay duplicate detection (operationId type mismatch versus stored receipt). DeviceId={DeviceId}, OperationId={OperationId}, StoredType={StoredType}, RequestedType={RequestedType}",
                        deviceId,
                        opId,
                        existing.OperationType,
                        operationType);
                }

                if (DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(operationType) ||
                    DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(existing.OperationType))
                {
                    _logger.LogWarning(
                        "Customer/shift sync durable replay visibility: replay duplicate detection (operationId type mismatch versus stored receipt). DeviceId={DeviceId}, OperationId={OperationId}, StoredType={StoredType}, RequestedType={RequestedType}",
                        deviceId,
                        opId,
                        existing.OperationType,
                        operationType);
                }

                await _syncConflictRecorder.RecordAsync(
                    new SyncConflictRecordRequest
                    {
                        DeviceId = deviceId,
                        OperationId = opId,
                        OperationType = operationType,
                        EntityType = "SyncOperation",
                        ConflictType = SyncConflictTypes.ReplayMismatch,
                        Reason =
                            $"operationId already recorded for a different operation type (stored={existing.OperationType}, requested={operationType})",
                        DedupeByDeviceOperationAndType = true
                    },
                    cancellationToken);

                await _operationalAuditRecorder.RecordAsync(
                    new OperationalAuditRecordRequest
                    {
                        Category = OperationalAuditCategories.Replay,
                        Action = OperationalAuditActions.ReplayMismatch,
                        EntityType = "SyncOperation",
                        DeviceId = deviceId,
                        OperationId = opId,
                        CorrelationId = opId,
                        Severity = OperationalAuditSeverity.Warning,
                        Summary = "Durable replay operation type mismatch",
                        DedupeByDeviceOperationAndAction = true,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["storedType"] = existing.OperationType,
                            ["requestedType"] = operationType
                        }
                    },
                    cancellationToken);

                await tx.CommitAsync(cancellationToken);
                return new OpResultDto
                {
                    OpId = opId,
                    Success = false,
                    Message = "operationId already recorded for a different operation type"
                };
            }

            _logger.LogInformation(
                "Sync durable replay short-circuit (DeviceId + OperationId match). DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, PriorSuccess={PriorSuccess}, PriorConflict={PriorConflict}",
                deviceId,
                opId,
                operationType,
                existing.Success,
                existing.Conflict);

            if (DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType))
            {
                _logger.LogInformation(
                    "Inventory sync durable replay visibility: replay short-circuit (no duplicate inventory mutation). DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, PriorSuccess={PriorSuccess}, PriorConflict={PriorConflict}",
                    deviceId,
                    opId,
                    operationType,
                    existing.Success,
                    existing.Conflict);
            }

            if (DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(operationType))
            {
                _logger.LogInformation(
                    "Customer/shift sync durable replay visibility: replay short-circuit (no duplicate placeholder processor invocation). DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, PriorSuccess={PriorSuccess}, PriorConflict={PriorConflict}",
                    deviceId,
                    opId,
                    operationType,
                    existing.Success,
                    existing.Conflict);
            }

            _executionScope.MarkReplayShortCircuited();
            await _operationalAuditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Replay,
                    Action = OperationalAuditActions.DurableReplayShortCircuit,
                    EntityType = "SyncOperation",
                    DeviceId = deviceId,
                    OperationId = opId,
                    CorrelationId = opId,
                    Severity = OperationalAuditSeverity.Information,
                    Summary = "Durable replay short-circuit (prior receipt matched)",
                    DedupeByDeviceOperationAndAction = true,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["operationType"] = operationType,
                        ["priorSuccess"] = existing.Success,
                        ["priorConflict"] = existing.Conflict
                    }
                },
                cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Map(existing);
        }

        if (DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType))
        {
            _logger.LogInformation(
                "Inventory sync durable replay visibility: executing inventory mutation under durable transaction boundary. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}",
                deviceId,
                opId,
                operationType);
        }

        if (DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(operationType))
        {
            _logger.LogInformation(
                "Customer/shift sync durable replay visibility: executing placeholder processor under durable transaction boundary. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}",
                deviceId,
                opId,
                operationType);
        }

        var result = await operation();

        if (result.Success || result.Conflict)
        {
            // Governance: durable replay receipt row records the first successful/conflict outcome for DeviceId+OperationId (same EF transaction as outer scope when handlers join CurrentTransaction).
            _db.SyncOperationReceipts.Add(new SyncOperationReceipt
            {
                DeviceId = deviceId,
                OperationId = opId,
                OperationType = operationType,
                Success = result.Success,
                Conflict = result.Conflict,
                ServerId = result.ServerId,
                ResultMessage = result.Message,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sync durable replay governance: receipt persisted after processor success. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, Success={Success}, Conflict={Conflict}",
                deviceId,
                opId,
                operationType,
                result.Success,
                result.Conflict);

            if (DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType))
            {
                _logger.LogInformation(
                    "Inventory sync durable replay visibility: durable replay receipt persisted after inventory processor success. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, Success={Success}, Conflict={Conflict}",
                    deviceId,
                    opId,
                    operationType,
                    result.Success,
                    result.Conflict);
            }

            if (DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(operationType))
            {
                _logger.LogInformation(
                    "Customer/shift sync durable replay visibility: durable replay receipt persisted after placeholder processor success. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, Success={Success}, Conflict={Conflict}",
                    deviceId,
                    opId,
                    operationType,
                    result.Success,
                    result.Conflict);
            }
        }

        await tx.CommitAsync(cancellationToken);
        return result;
    }

    private static OpResultDto Map(SyncOperationReceipt r) =>
        new()
        {
            OpId = r.OperationId,
            Success = r.Success,
            Conflict = r.Conflict,
            ServerId = r.ServerId,
            Message = r.ResultMessage
        };
}
