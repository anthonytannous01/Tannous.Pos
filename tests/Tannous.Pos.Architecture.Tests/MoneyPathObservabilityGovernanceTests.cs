using System.Text.RegularExpressions;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Observability-focused substring anchors for finalize, void, and sync money paths (governance visibility only).
/// </summary>
public class MoneyPathObservabilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrder_observability_logs_and_transaction_anchors_present()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("LogWarning", text, StringComparison.Ordinal);
        Assert.Contains("recomputed from lines differs from persisted SubTotal", text, StringComparison.Ordinal);
        Assert.Contains("Order already finalized", text, StringComparison.Ordinal);
        Assert.Contains("short-circuit", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BeginTransactionAsync", text, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", text, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrder_observability_documents_paid_void_inventory_reversal()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("Inventory reversal observability:", text, StringComparison.Ordinal);
        Assert.Contains("GOVERNANCE / RISK", text, StringComparison.Ordinal);
        Assert.Contains("inventory", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_money_processors_carry_replay_idempotency_and_governance_markers()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var proc in new[] { "CreateOrder", "FinalizeOrder", "CashDrop" })
        {
            var body = SyncControllerProcessBodyExtractor.ExtractProcessBody(text, proc);
            Assert.False(string.IsNullOrEmpty(body), $"Expected Process{proc} method body in SyncController.cs");

            Assert.True(
                body.Contains("GOVERNANCE / RISK", StringComparison.Ordinal),
                $"Process{proc}: missing GOVERNANCE / RISK marker.");
            Assert.True(
                Regex.IsMatch(body, @"\b(replay|retry|duplicate|double-apply|operationId|OpId)\b", RegexOptions.IgnoreCase),
                $"Process{proc}: missing replay / duplicate / correlation wording (e.g. operationId or OpId).");
            Assert.True(
                Regex.IsMatch(body, @"\b(idempotency|idempotent|operationId)\b", RegexOptions.IgnoreCase),
                $"Process{proc}: missing idempotency / operationId correlation wording.");
            Assert.Contains("_mediator.Send", body, StringComparison.Ordinal);
        }
    }

}
