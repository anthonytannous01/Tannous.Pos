namespace Tannous.Pos.Application.Sync;

/// <summary>Internal operator-driven sync conflict reconciliation workflow (diagnostics only; no auto-healing).</summary>
public interface ISyncConflictReconciliationService
{
    Task<SyncConflictPageDto> GetUnresolvedAsync(
        SyncConflictQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SyncConflictPageDto> GetByStatusAsync(
        string resolutionStatus,
        SyncConflictQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReconciliationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<SyncConflictItemDto> AcknowledgeAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default);

    Task<SyncConflictItemDto> InvestigateAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default);

    Task<SyncConflictItemDto> ResolveAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default);

    Task<SyncConflictItemDto> IgnoreAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default);
}
