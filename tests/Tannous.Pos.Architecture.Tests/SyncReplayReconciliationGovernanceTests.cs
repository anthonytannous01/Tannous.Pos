using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for sync replay reconciliation visibility and replay-protected placeholder processors.
/// </summary>
public class SyncReplayReconciliationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void SyncController_push_has_reconciliation_visibility_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync reconciliation visibility: replay mixed with failed operations", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation visibility: mixed placeholder and replay short-circuit in batch", text, StringComparison.Ordinal);
        Assert.Contains("Sync reconciliation visibility: mixed inventory replay and failed operations in batch", text, StringComparison.Ordinal);
        Assert.Contains("ReplayCount", text, StringComparison.Ordinal);
        Assert.Contains("HasReplayMixedWithFailureOrConflict", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_protected_placeholder_processors_retain_governance_and_placeholder_wording()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var method in new[] { "ProcessCreateCustomer", "ProcessOpenShift" })
        {
            var start = text.IndexOf($"async Task<OpResultDto> {method}", StringComparison.Ordinal);
            if (start < 0)
                start = text.IndexOf($"Task<OpResultDto> {method}", StringComparison.Ordinal);
            Assert.True(start >= 0, $"Missing {method}");
            var end = text.IndexOf("private ", start + 10, StringComparison.Ordinal);
            if (end < 0)
                end = text.Length;
            var body = text.Substring(start, end - start);
            Assert.Contains("GOVERNANCE / RISK", body, StringComparison.Ordinal);
            Assert.Contains("Placeholder success", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("placeholder-only", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("durable SyncOperationReceipt", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SyncPushBatchTelemetry_tracks_customer_shift_and_inventory_replay_short_circuits()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncPushBatchTelemetry.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("CustomerShiftReplayShortCircuitCount", text, StringComparison.Ordinal);
        Assert.Contains("InventoryReplayShortCircuitCount", text, StringComparison.Ordinal);
        Assert.Contains("IsCustomerOrShiftProtected", text, StringComparison.Ordinal);
        Assert.Contains("IsInventoryProtected", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncOperationOutcomeClassifier_still_classifies_placeholder_types_for_first_run()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncOperationOutcomeClassifier.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("CreateCustomer", text, StringComparison.Ordinal);
        Assert.Contains("OpenShift", text, StringComparison.Ordinal);
        Assert.Contains("PlaceholderOperation", text, StringComparison.Ordinal);
        Assert.Contains("replayShortCircuited", text, StringComparison.OrdinalIgnoreCase);
    }
}
