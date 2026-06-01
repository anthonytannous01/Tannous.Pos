using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for durable sync replay (SyncOperationReceipt entity) and protected operation types.
/// </summary>
public class DurableSyncReplayGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void PosDbContextModelSnapshot_includes_SyncOperationReceipt_with_unique_device_operation_index()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "PosDbContextModelSnapshot.cs");
        var text = File.ReadAllText(path);
        var start = text.IndexOf("Tannous.Pos.Domain.Entities.SyncOperationReceipt", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = text.IndexOf("modelBuilder.Entity(\"Tannous.Pos.Domain.Entities.User\"", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var slice = text.Substring(start, end - start);
        Assert.Contains("HasIndex(\"DeviceId\", \"OperationId\")", slice, StringComparison.Ordinal);
        Assert.Contains(".IsUnique()", slice, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSyncOperationReceipts_migration_exists()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260514100000_AddSyncOperationReceipts.cs");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("SyncOperationReceipts", text, StringComparison.Ordinal);
        Assert.Contains("DeviceId", text, StringComparison.Ordinal);
        Assert.Contains("OperationId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayProtectedTypes_includes_all_known_push_operation_types()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "DurableSyncReplayProtectedTypes.cs");
        var text = File.ReadAllText(path);
        foreach (var t in new[]
        {
            "CreateOrder",
            "FinalizeOrder",
            "CashDrop",
            "AdjustInventory",
            "RecordWastage",
            "OpenShift",
            "CreateCustomer"
        })
        {
            Assert.Contains($"\"{t}\"", text, StringComparison.Ordinal);
        }

        Assert.Contains("IsInventoryProtected", text, StringComparison.Ordinal);
        Assert.Contains("IsCustomerOrShiftProtected", text, StringComparison.Ordinal);
        Assert.Contains("AdjustInventory", text, StringComparison.Ordinal);
        Assert.Contains("RecordWastage", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayProtectedTypes_registry_matches_SyncController_ExecuteAsync_parity()
    {
        var typesPath = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "DurableSyncReplayProtectedTypes.cs");
        var syncPath = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var typesText = File.ReadAllText(typesPath);
        var syncText = File.ReadAllText(syncPath);

        var protectedTypes = new[] { "CreateOrder", "FinalizeOrder", "CashDrop", "AdjustInventory", "RecordWastage", "OpenShift", "CreateCustomer" };
        foreach (var t in protectedTypes)
        {
            Assert.Contains($"\"{t}\"", typesText, StringComparison.Ordinal);
            Assert.Contains($"case \"{t}\":", syncText, StringComparison.Ordinal);
        }

        var executeCount = System.Text.RegularExpressions.Regex.Matches(syncText, @"_replayCoordinator\.ExecuteAsync\s*\(").Count;
        Assert.Equal(protectedTypes.Length, executeCount);
    }

    [Fact]
    public void SyncController_invokes_replay_coordinator_for_all_seven_protected_operation_types()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        var executeCount = System.Text.RegularExpressions.Regex.Matches(text, @"_replayCoordinator\.ExecuteAsync\s*\(").Count;
        Assert.Equal(7, executeCount);
        Assert.Contains("case \"CreateOrder\":", text, StringComparison.Ordinal);
        Assert.Contains("case \"FinalizeOrder\":", text, StringComparison.Ordinal);
        Assert.Contains("case \"CashDrop\":", text, StringComparison.Ordinal);
        Assert.Contains("case \"RecordWastage\":", text, StringComparison.Ordinal);
        Assert.Contains("() => ProcessRecordWastage", text, StringComparison.Ordinal);
        Assert.Contains("case \"AdjustInventory\":", text, StringComparison.Ordinal);
        Assert.Contains("() => ProcessAdjustInventory", text, StringComparison.Ordinal);
        Assert.Contains("case \"OpenShift\":", text, StringComparison.Ordinal);
        Assert.Contains("() => ProcessOpenShift", text, StringComparison.Ordinal);
        Assert.Contains("case \"CreateCustomer\":", text, StringComparison.Ordinal);
        Assert.Contains("() => ProcessCreateCustomer", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayCoordinator_persists_receipt_after_processor_success_path()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "DurableSyncReplayCoordinator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("SyncOperationReceipts.Add", text, StringComparison.Ordinal);
        Assert.Contains("if (result.Success || result.Conflict)", text, StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("await operation()", StringComparison.Ordinal) <
            text.IndexOf("SyncOperationReceipts.Add", StringComparison.Ordinal),
            "Receipt add must follow processor invocation.");
        Assert.Contains("Sync durable replay short-circuit", text, StringComparison.Ordinal);
        Assert.Contains("Sync durable replay governance: receipt persisted after processor success", text, StringComparison.Ordinal);
        Assert.Contains("Inventory sync durable replay visibility: replay short-circuit (no duplicate inventory mutation).", text, StringComparison.Ordinal);
        Assert.Contains("Inventory sync durable replay visibility: durable replay receipt persisted after inventory processor success.", text, StringComparison.Ordinal);
        Assert.Contains("Inventory sync durable replay visibility: executing inventory mutation under durable transaction boundary.", text, StringComparison.Ordinal);
        Assert.Contains("Inventory sync durable replay visibility: replay duplicate detection (operationId type mismatch versus stored receipt).", text, StringComparison.Ordinal);
        Assert.Contains("Customer/shift sync durable replay visibility: replay short-circuit (no duplicate placeholder processor invocation).", text, StringComparison.Ordinal);
        Assert.Contains("Customer/shift sync durable replay visibility: durable replay receipt persisted after placeholder processor success.", text, StringComparison.Ordinal);
        Assert.Contains("Customer/shift sync durable replay visibility: replay duplicate detection (operationId type mismatch versus stored receipt).", text, StringComparison.Ordinal);
        Assert.Contains("IsInventoryProtected", text, StringComparison.Ordinal);
        Assert.Contains("IsCustomerOrShiftProtected", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_IDurableSyncReplayCoordinator()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IDurableSyncReplayCoordinator", text, StringComparison.Ordinal);
        Assert.Contains("DurableSyncReplayCoordinator", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayCoordinator_logs_short_circuit_and_missing_correlation()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "DurableSyncReplayCoordinator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync durable replay short-circuit", text, StringComparison.Ordinal);
        Assert.Contains("Sync durable replay disabled", text, StringComparison.Ordinal);
        Assert.Contains("operationId reused", text, StringComparison.OrdinalIgnoreCase);
    }
}
