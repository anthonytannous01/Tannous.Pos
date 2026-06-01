namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Best-effort persistence of sync reconciliation conflicts. Failures must never propagate to callers.
/// </summary>
public interface ISyncConflictRecorder
{
    Task RecordAsync(SyncConflictRecordRequest request, CancellationToken cancellationToken = default);
}
