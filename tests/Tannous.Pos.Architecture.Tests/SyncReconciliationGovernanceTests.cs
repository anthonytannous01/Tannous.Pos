using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for sync reconciliation conflict tracking and observability.
/// </summary>
public class SyncReconciliationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Sync_conflict_recorder_service_exists_and_is_registered()
    {
        var iface = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "ISyncConflictRecorder.cs");
        var impl = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictRecorder.cs");
        var program = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");

        Assert.True(File.Exists(iface), "Missing ISyncConflictRecorder");
        Assert.True(File.Exists(impl), "Missing SyncConflictRecorder");
        var programText = File.ReadAllText(program);
        Assert.Contains("ISyncConflictRecorder", programText, StringComparison.Ordinal);
        Assert.Contains("SyncConflictRecorder", programText, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncConflictTypes_contains_required_conflict_classifications()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncConflictTypes.cs");
        var text = File.ReadAllText(path);
        foreach (var type in new[]
                 {
                     "ConcurrencyConflict",
                     "ReplayMismatch",
                     "StaleOfflineMutation",
                     "InventoryDriftRisk",
                     "LifecycleStateConflict"
                 })
        {
            Assert.Contains(type, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SyncConflictRecord_entity_and_migration_present()
    {
        var entity = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "SyncConflictRecord.cs");
        var migration = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260516160000_AddSyncConflictRecords.cs");
        var snapshot = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "PosDbContextModelSnapshot.cs");

        Assert.True(File.Exists(entity), "Missing SyncConflictRecord entity");
        Assert.True(File.Exists(migration), "Missing AddSyncConflictRecords migration");
        var snapshotText = File.ReadAllText(snapshot);
        Assert.Contains("SyncConflictRecords", snapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_reconciliation_observability_anchors_exist_in_recorder()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictRecorder.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync reconciliation observability: conflict recorded", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation observability: replay mismatch conflict", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation observability: stale offline mutation", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation observability: lifecycle state conflict", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation observability: inventory drift risk", text, StringComparison.Ordinal);
        Assert.Contains("conflict record persistence failed (best-effort", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DurableSyncReplayCoordinator_records_replay_mismatch_conflicts()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "DurableSyncReplayCoordinator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ISyncConflictRecorder", text, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.ReplayMismatch", text, StringComparison.Ordinal);
        Assert.Contains("DedupeByDeviceOperationAndType", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalExceptionHandler_records_concurrency_conflicts_best_effort()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("RecordConcurrencyConflictBestEffortAsync", text, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.ConcurrencyConflict", text, StringComparison.Ordinal);
        Assert.Contains("ISyncConflictRecorder", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Finalize_and_void_handlers_use_sync_conflict_recorder()
    {
        var finalize = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs"));
        var voidHandler = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs"));

        Assert.Contains("ISyncConflictRecorder", finalize, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.StaleOfflineMutation", finalize, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.ConcurrencyConflict", finalize, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.InventoryDriftRisk", finalize, StringComparison.Ordinal);

        Assert.Contains("ISyncConflictRecorder", voidHandler, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.LifecycleStateConflict", voidHandler, StringComparison.Ordinal);
        Assert.Contains("SyncConflictTypes.ConcurrencyConflict", voidHandler, StringComparison.Ordinal);
    }
}
