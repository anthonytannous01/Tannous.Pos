using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Prevents accidental removal of idempotency / replay / partial-batch observability anchors (substring-tolerant).
/// </summary>
public class ReplayIdempotencyIntegrityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrderCommandHandler_retains_idempotency_replay_warning_anchor()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Finalize idempotency observability: duplicate finalize or replay attempt", text, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey={IdempotencyKey}", text, StringComparison.Ordinal);
        Assert.Contains("Finalize governance: short-circuit on Paid order", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_retains_duplicate_operationId_and_classification_logs()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync replay visibility: duplicate operationId within same batch", text, StringComparison.Ordinal);
        Assert.Contains("ReplayRisk=money-or-inventory-if-payload-differs", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_retains_money_and_inventory_replay_visibility_classifications()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("replay visibility classification=money-affecting", text, StringComparison.Ordinal);
        Assert.Contains("replay visibility classification=inventory-affecting", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_retains_placeholder_only_replay_visibility_warnings()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync replay visibility: placeholder-only processor (CreateCustomer)", text, StringComparison.Ordinal);
        Assert.Contains("Sync replay visibility: placeholder-only processor (OpenShift)", text, StringComparison.Ordinal);
        Assert.Contains("ReplayClass=placeholder-only", text, StringComparison.Ordinal);
    }
}
