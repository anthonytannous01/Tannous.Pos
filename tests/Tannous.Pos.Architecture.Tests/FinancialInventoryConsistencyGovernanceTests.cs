using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for inventory/money consistency observability on finalize/void and Sync replay sensitivity classification.
/// </summary>
public class FinancialInventoryConsistencyGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrderCommandHandler_documents_inventory_consistency_observability_and_movements()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Inventory consistency observability:", text, StringComparison.Ordinal);
        Assert.Contains("finalize inventory deduction pass starting", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("negative stock after finalize sale deduction", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InventoryItemId={InventoryItemId}", text, StringComparison.Ordinal);
        Assert.Contains("AddMovementAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_paid_void_reversal_observability_includes_idempotency_key()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Inventory reversal observability:", text, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey={IdempotencyKey}", text, StringComparison.Ordinal);
        Assert.Contains("GOVERNANCE / RISK: Paid void restores inventory from finalize Sale movements only", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_each_replay_sensitive_Process_method_declares_replay_sensitivity_classification()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var proc in new[] { "CreateCustomer", "CreateOrder", "FinalizeOrder", "OpenShift", "CashDrop", "RecordWastage", "AdjustInventory" })
        {
            var body = SyncControllerProcessBodyExtractor.ExtractProcessBody(text, proc);
            Assert.False(string.IsNullOrEmpty(body), $"Expected Process{proc} body.");
            Assert.Contains("Replay sensitivity classification:", body, StringComparison.Ordinal);
        }
    }
}
