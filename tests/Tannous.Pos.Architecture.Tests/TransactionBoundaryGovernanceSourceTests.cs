using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for transaction and void persistence paths (no runtime behavior change).
/// </summary>
public class TransactionBoundaryGovernanceSourceTests
{
    [Fact]
    public void VoidOrderCommandHandler_persists_void_status_via_unit_of_work()
    {
        var repoRoot = ObservabilitySourceGovernanceTests.RepoRoot();
        var path = Path.Combine(repoRoot, "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("order.Status = OrderStatus.Void", text, StringComparison.Ordinal);
        Assert.Contains("SaveChangesAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_documents_paid_void_reversal_governance()
    {
        var repoRoot = ObservabilitySourceGovernanceTests.RepoRoot();
        var path = Path.Combine(repoRoot, "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / RISK", text, StringComparison.Ordinal);
        Assert.Contains("Inventory reversal observability:", text, StringComparison.Ordinal);
        Assert.Contains("no recipe recompute", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SyncController_push_path_logs_missing_operationId_for_replay_visibility()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("operation missing operationId", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sync replay visibility:", text, StringComparison.Ordinal);
        Assert.Contains("replay/idempotency", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SyncController_push_path_logs_partial_batch_replay_visibility()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync replay visibility: partial application", text, StringComparison.Ordinal);
    }
}
