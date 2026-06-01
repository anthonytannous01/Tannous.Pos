using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Documents sync push replay / placeholder governance in source (visibility over fake safety).
/// </summary>
public class SyncReplayGovernanceSourceTests
{
    [Fact]
    public void Sync_push_logs_duplicate_operationId_within_batch()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("duplicate operationId within same batch", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sync_placeholder_processors_carry_governance_risk_comments()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / RISK", text, StringComparison.Ordinal);
        Assert.Contains("ProcessFinalizeOrder", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_push_logs_partial_batch_failure_visibility()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Sync replay visibility: partial application", text, StringComparison.Ordinal);
    }
}
