using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for append-only operational audit persistence (not event sourcing).
/// </summary>
public class OperationalAuditGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Operational_audit_recorder_exists_and_is_registered()
    {
        var iface = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "IOperationalAuditRecorder.cs");
        var impl = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditRecorder.cs");
        var program = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");

        Assert.True(File.Exists(iface), "Missing IOperationalAuditRecorder");
        Assert.True(File.Exists(impl), "Missing OperationalAuditRecorder");
        var programText = File.ReadAllText(program);
        Assert.Contains("IOperationalAuditRecorder", programText, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditRecorder", programText, StringComparison.Ordinal);
    }

    [Fact]
    public void OperationalAuditRecord_entity_and_migration_present()
    {
        var entity = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "OperationalAuditRecord.cs");
        var migration = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260517120000_AddOperationalAuditRecords.cs");

        Assert.True(File.Exists(entity), "Missing OperationalAuditRecord entity");
        Assert.True(File.Exists(migration), "Missing AddOperationalAuditRecords migration");
        var entityText = File.ReadAllText(entity);
        Assert.Contains("CreatedAtUtc", entityText, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdatedAt", entityText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Operational_audit_categories_and_actions_cover_required_events()
    {
        var categories = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditCategories.cs"));
        var actions = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditActions.cs"));

        foreach (var category in new[] { "Order", "Inventory", "Settlement", "Replay", "Reconciliation", "Refund", "Concurrency" })
            Assert.Contains(category, categories, StringComparison.Ordinal);

        foreach (var action in new[]
                 {
                     "FinalizeSuccess",
                     "FinalizeReplayShortCircuit",
                     "VoidSuccess",
                     "RefundPersisted",
                     "SettlementOverpayment",
                     "SettlementUnderpaymentRejected",
                     "InventoryDeductionPass",
                     "NegativeStockDetected",
                     "ReversalMovementPersisted",
                     "ReplayMismatch",
                     "StaleOfflineMutation",
                     "LifecycleStateConflict",
                     "PartialBatchReconciliation",
                     "ConcurrencyConflict",
                     "DurableReplayShortCircuit",
                     "PlaceholderOperationExecuted",
                     "MixedBatchOutcomes"
                 })
        {
            Assert.Contains(action, actions, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Operational_audit_observability_anchors_exist_in_recorder()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditRecorder.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational audit observability: audit persisted", text, StringComparison.Ordinal);
        Assert.Contains("Operational audit observability: persistence failure", text, StringComparison.Ordinal);
        Assert.Contains("best-effort", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Operational_audit_recorder_uses_isolated_scope_for_persistence()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditRecorder.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IServiceScopeFactory", text, StringComparison.Ordinal);
        Assert.Contains("CreateAsyncScope", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayCoordinator_records_operational_audit_for_replay_events()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "DurableSyncReplayCoordinator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalAuditRecorder", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.ReplayMismatch", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.DurableReplayShortCircuit", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Finalize_and_void_handlers_use_operational_audit_recorder()
    {
        var finalize = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs"));
        var voidHandler = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs"));

        Assert.Contains("IOperationalAuditRecorder", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.FinalizeSuccess", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.SettlementOverpayment", finalize, StringComparison.Ordinal);

        Assert.Contains("IOperationalAuditRecorder", voidHandler, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.VoidSuccess", voidHandler, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.RefundPersisted", voidHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_records_operational_audit_for_batch_and_placeholder_paths()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalAuditRecorder", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.MixedBatchOutcomes", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.PartialBatchReconciliation", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.PlaceholderOperationExecuted", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalExceptionHandler_records_operational_concurrency_audit_best_effort()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalAuditRecorder", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.ConcurrencyConflict", text, StringComparison.Ordinal);
    }
}
