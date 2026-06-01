using Tannous.Pos.Application.OperationalReconciliation;

namespace Tannous.Pos.Application.Audit;

/// <summary>Read-only paginated queries over append-only operational audit records (internal diagnostics).</summary>
public interface IOperationalAuditQueryService
{
    Task<OperationalAuditPageDto> GetOrderTimelineAsync(
        Guid orderId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalAuditPageDto> GetDeviceTimelineAsync(
        string deviceId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalAuditPageDto> GetOperationTimelineAsync(
        string operationId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalAuditPageDto> GetEntityTimelineAsync(
        string entityType,
        Guid entityId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalAuditPageDto> GetRecentConflictsAsync(
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalAuditPageDto> GetReconciliationWorkflowAuditAsync(
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalOrderAuditSummaryDto> GetOrderAuditSummaryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OperationalDeviceAuditSummaryDto> GetDeviceAuditSummaryAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent Warning or Critical audit records for the given order,
    /// ordered descending by timestamp. TopN is clamped to [1, 10].
    /// </summary>
    Task<IReadOnlyList<OperationalAuditTimelineItemDto>> GetOrderAuditHighlightsAsync(
        Guid orderId,
        int topN,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent Warning or Critical audit records for the given device,
    /// ordered descending by timestamp. TopN is clamped to [1, 10].
    /// </summary>
    Task<IReadOnlyList<OperationalAuditTimelineItemDto>> GetDeviceAuditHighlightsAsync(
        string deviceId,
        int topN,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate counts for the reconciliation system: total unresolved conflicts,
    /// oldest unresolved conflict timestamp, and order-scoped unresolved count.
    /// Uses three sequential EF queries against SyncConflictRecords.
    /// </summary>
    Task<OperationalReconciliationAuditSummaryDto> GetReconciliationSystemSummaryAsync(
        CancellationToken cancellationToken = default);
}
