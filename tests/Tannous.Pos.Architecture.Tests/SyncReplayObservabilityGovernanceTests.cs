using System.Text.RegularExpressions;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source-level governance for sync replay/idempotency commentary, partial-batch visibility, and targeted MediatR delegation.
/// </summary>
public class SyncReplayObservabilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Replay_sensitive_Process_methods_contain_replay_idempotency_and_governance_markers()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var proc in new[] { "CreateOrder", "FinalizeOrder", "OpenShift", "CashDrop", "RecordWastage", "AdjustInventory" })
        {
            var body = SyncControllerProcessBodyExtractor.ExtractProcessBody(text, proc);
            Assert.False(string.IsNullOrEmpty(body), $"Expected Process{proc} method body in SyncController.cs");

            Assert.Contains("GOVERNANCE / RISK", body, StringComparison.Ordinal);
            Assert.True(
                Regex.IsMatch(body, @"\b(replay|retry|duplicate|double-apply|operationId|OpId)\b", RegexOptions.IgnoreCase),
                $"Process{proc}: expected replay / correlation wording (e.g. replay, duplicate, operationId).");
            Assert.True(
                Regex.IsMatch(body, @"\b(idempotency|idempotent|operationId)\b", RegexOptions.IgnoreCase),
                $"Process{proc}: expected idempotency or operationId correlation wording.");
        }
    }

    [Fact]
    public void Sync_push_surface_documents_partial_apply_mixed_success_and_reconciliation_review()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        Assert.True(
            text.Contains("partial application", StringComparison.OrdinalIgnoreCase)
                || text.Contains("partial apply", StringComparison.OrdinalIgnoreCase),
            "Expected partial application / partial apply visibility (log or comment) in SyncController.");
        Assert.True(
            text.Contains("mixed success", StringComparison.OrdinalIgnoreCase)
                || text.Contains("mixed outcomes", StringComparison.OrdinalIgnoreCase),
            "Expected mixed success / mixed outcomes commentary for batch handling in SyncController.");
        Assert.True(
            text.Contains("reconciliation", StringComparison.OrdinalIgnoreCase)
                || text.Contains("manual review", StringComparison.OrdinalIgnoreCase),
            "Expected reconciliation or manual review governance wording in SyncController.");
    }

    [Fact]
    public void Migrated_money_processors_delegate_to_mediatr_and_non_migrated_keep_placeholder_wording()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var proc in new[] { "CreateOrder", "FinalizeOrder", "CashDrop" })
        {
            var body = SyncControllerProcessBodyExtractor.ExtractProcessBody(text, proc);
            Assert.False(string.IsNullOrEmpty(body), $"Expected Process{proc} body.");
            Assert.True(
                body.Contains("_mediator.Send", StringComparison.Ordinal),
                $"Process{proc}: expected MediatR command delegation.");
            Assert.DoesNotContain("Placeholder success", body, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var proc in new[] { "OpenShift", "CreateCustomer" })
        {
            var body = SyncControllerProcessBodyExtractor.ExtractProcessBody(text, proc);
            Assert.False(string.IsNullOrEmpty(body), $"Expected Process{proc} body.");
            Assert.Contains("Placeholder", body, StringComparison.OrdinalIgnoreCase);
        }
    }
}
