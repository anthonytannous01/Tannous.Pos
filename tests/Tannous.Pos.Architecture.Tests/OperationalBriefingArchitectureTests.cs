using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalBriefingArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Briefing_service_reads_stores_not_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalBriefingService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalEquilibriumSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalStrategySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAttentionSnapshotStore", text, StringComparison.Ordinal);

        Assert.DoesNotContain("IOperationalEquilibriumService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalStrategyService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalAttentionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalRecoveryService", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Briefing_has_no_snapshot_store_of_its_own()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services");
        var briefingStores = Directory
            .GetFiles(dir, "*Briefing*SnapshotStore*", SearchOption.AllDirectories);

        Assert.Empty(briefingStores);
    }

    [Fact]
    public void Briefing_aggregation_is_deterministic()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalBriefing",
            "OperationalBriefingAggregation.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("System.Reflection", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", text, StringComparison.Ordinal);
        Assert.DoesNotContain("async", text, StringComparison.Ordinal);
        Assert.DoesNotContain("await", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Briefing_controller_follows_versioned_internal_route()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditBriefingController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("api/v{version:apiVersion}/internal/operational-audit/briefing", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("HttpGet", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPut", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Briefing_service_is_not_async_io()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalBriefingService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("Task.FromResult", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", text, StringComparison.Ordinal);
    }
}
