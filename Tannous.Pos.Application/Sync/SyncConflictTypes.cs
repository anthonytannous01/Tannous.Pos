namespace Tannous.Pos.Application.Sync;

/// <summary>Internal sync reconciliation conflict classification (operational diagnostics only).</summary>
public static class SyncConflictTypes
{
    public const string ConcurrencyConflict = "ConcurrencyConflict";
    public const string ReplayMismatch = "ReplayMismatch";
    public const string StaleOfflineMutation = "StaleOfflineMutation";
    public const string InventoryDriftRisk = "InventoryDriftRisk";
    public const string LifecycleStateConflict = "LifecycleStateConflict";
}
