using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class SyncConflictRecorder : ISyncConflictRecorder
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncConflictRecorder> _logger;

    public SyncConflictRecorder(IServiceScopeFactory scopeFactory, ILogger<SyncConflictRecorder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RecordAsync(SyncConflictRecordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();

            if (request.DedupeByDeviceOperationAndType &&
                !string.IsNullOrWhiteSpace(request.DeviceId) &&
                !string.IsNullOrWhiteSpace(request.OperationId) &&
                !string.IsNullOrWhiteSpace(request.ConflictType))
            {
                var exists = await db.SyncConflictRecords.AsNoTracking().AnyAsync(
                    r => r.DeviceId == request.DeviceId
                         && r.OperationId == request.OperationId
                         && r.ConflictType == request.ConflictType
                         && (r.ResolutionStatus == ReconciliationResolutionStatus.Unresolved.ToString()
                             || r.ResolutionStatus == ReconciliationResolutionStatus.Acknowledged.ToString()
                             || r.ResolutionStatus == ReconciliationResolutionStatus.Investigating.ToString()),
                    cancellationToken);
                if (exists)
                    return;
            }

            var record = new SyncConflictRecord
            {
                DeviceId = request.DeviceId,
                OperationId = request.OperationId,
                OperationType = request.OperationType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                ConflictType = request.ConflictType,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId,
                CreatedAtUtc = DateTime.UtcNow,
                Resolved = false,
                ResolutionStatus = ReconciliationResolutionStatus.Unresolved.ToString()
            };

            db.SyncConflictRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken);

            LogConflictObservability(request);

            var invalidator = scope.ServiceProvider.GetService<IOperationalDiagnosticsCacheInvalidator>();
            invalidator?.InvalidateAfterConflictRecorded(
                request.ConflictType,
                request.DeviceId,
                request.OperationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Sync reconciliation observability: conflict record persistence failed (best-effort; business path continues). ConflictType={ConflictType}, DeviceId={DeviceId}, OperationId={OperationId}",
                request.ConflictType,
                request.DeviceId,
                request.OperationId);
        }
    }

    private void LogConflictObservability(SyncConflictRecordRequest request)
    {
        _logger.LogWarning(
            "Sync reconciliation observability: conflict recorded. DeviceId={DeviceId}, OperationId={OperationId}, OperationType={OperationType}, EntityType={EntityType}, EntityId={EntityId}, ConflictType={ConflictType}, CorrelationId={CorrelationId}, Reason={Reason}",
            request.DeviceId,
            request.OperationId,
            request.OperationType,
            request.EntityType,
            request.EntityId,
            request.ConflictType,
            request.CorrelationId,
            request.Reason);

        switch (request.ConflictType)
        {
            case SyncConflictTypes.ReplayMismatch:
                _logger.LogWarning(
                    "Sync reconciliation observability: replay mismatch conflict. DeviceId={DeviceId}, OperationId={OperationId}, EntityType={EntityType}, EntityId={EntityId}, ConflictType={ConflictType}, CorrelationId={CorrelationId}",
                    request.DeviceId,
                    request.OperationId,
                    request.EntityType,
                    request.EntityId,
                    request.ConflictType,
                    request.CorrelationId);
                break;
            case SyncConflictTypes.StaleOfflineMutation:
                _logger.LogWarning(
                    "Sync reconciliation observability: stale offline mutation. DeviceId={DeviceId}, OperationId={OperationId}, EntityType={EntityType}, EntityId={EntityId}, ConflictType={ConflictType}, CorrelationId={CorrelationId}",
                    request.DeviceId,
                    request.OperationId,
                    request.EntityType,
                    request.EntityId,
                    request.ConflictType,
                    request.CorrelationId);
                break;
            case SyncConflictTypes.LifecycleStateConflict:
                _logger.LogWarning(
                    "Sync reconciliation observability: lifecycle state conflict. DeviceId={DeviceId}, OperationId={OperationId}, EntityType={EntityType}, EntityId={EntityId}, ConflictType={ConflictType}, CorrelationId={CorrelationId}",
                    request.DeviceId,
                    request.OperationId,
                    request.EntityType,
                    request.EntityId,
                    request.ConflictType,
                    request.CorrelationId);
                break;
            case SyncConflictTypes.InventoryDriftRisk:
                _logger.LogWarning(
                    "Sync reconciliation observability: inventory drift risk. DeviceId={DeviceId}, OperationId={OperationId}, EntityType={EntityType}, EntityId={EntityId}, ConflictType={ConflictType}, CorrelationId={CorrelationId}",
                    request.DeviceId,
                    request.OperationId,
                    request.EntityType,
                    request.EntityId,
                    request.ConflictType,
                    request.CorrelationId);
                break;
            case SyncConflictTypes.ConcurrencyConflict:
                break;
        }
    }
}
