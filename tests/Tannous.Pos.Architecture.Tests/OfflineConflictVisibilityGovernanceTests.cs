using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for offline/sync conflict visibility (inventory drift, lifecycle, partial batch).
/// </summary>
public class OfflineConflictVisibilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrderHandler_documents_inventory_drift_risk_on_negative_stock()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("negative stock after finalize sale deduction", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SyncConflictTypes.InventoryDriftRisk", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalizeOrderHandler_documents_stale_offline_finalize_on_void_order()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Finalize attempted on void order", text, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.StaleOfflineMutation", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderHandler_documents_lifecycle_state_conflict_on_invalid_void()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Void rejected: order status is", text, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.LifecycleStateConflict", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_records_partial_batch_inventory_reconciliation_conflicts()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("RecordBatchReconciliationConflictsAsync", text, StringComparison.Ordinal);
        Assert.Contains("HasPartialBatchInventoryReconciliation", text, StringComparison.Ordinal);
        Assert.Contains("HasMixedInventoryAndReplayVisibility", text, StringComparison.Ordinal);
        Assert.Contains("Partial batch: inventory operation failures", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncPushBatchTelemetry_tracks_inventory_operation_failures_for_partial_batch()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncPushBatchTelemetry.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("InventoryOperationFailureCount", text, StringComparison.Ordinal);
        Assert.Contains("HasPartialBatchInventoryReconciliation", text, StringComparison.Ordinal);
        Assert.Contains("IsInventoryProtected", text, StringComparison.Ordinal);
    }
}
