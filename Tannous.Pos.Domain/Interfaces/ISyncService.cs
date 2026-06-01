namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Legacy helpers used by <c>POST /api/sync</c> (simulate sync) and <c>GET /api/sync/status</c>.
/// Not authoritative for incremental pull/push; real sync state lives in <c>SyncController</c> pull/push handlers.
/// </summary>
public interface ISyncService
{
    Task<bool> SyncDataAsync();
    Task<DateTime> GetLastSyncTimeAsync();
    Task<bool> IsSyncRequiredAsync();
}
