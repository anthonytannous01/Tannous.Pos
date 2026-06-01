using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalHandoffArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Handoff_service_reads_stores_and_briefing_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalHandoffService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalEquilibriumSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalStrategySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAttentionSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalBriefingService", text, StringComparison.Ordinal);

        Assert.DoesNotContain("IOperationalEquilibriumService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalStrategyService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalAttentionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalRecoveryService", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Handoff_reads_full_snapshot_history_not_just_latest()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalHandoffService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("GetSnapshots()", text, StringComparison.Ordinal);
        Assert.DoesNotContain("LastOrDefault()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Handoff_has_no_snapshot_store_of_its_own()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services");
        var handoffStores = Directory
            .GetFiles(dir, "*Handoff*SnapshotStore*", SearchOption.AllDirectories);

        Assert.Empty(handoffStores);
    }

    [Fact]
    public void Handoff_aggregation_is_deterministic_and_uses_continuity_phrasing()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalHandoff",
            "OperationalHandoffAggregation.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("System.Reflection", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", text, StringComparison.Ordinal);
        Assert.DoesNotContain("async", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.Contains("OperationalContinuityPhrasing", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Handoff_controller_is_get_only_with_versioned_internal_route()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditHandoffController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("api/v{version:apiVersion}/internal/operational-audit/handoff",
            text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("HttpGet", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPut", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", text, StringComparison.Ordinal);
    }
}
