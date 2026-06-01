using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCompositionArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Workbench_services_depend_on_composition_hub_not_other_workbenches()
    {
        foreach (var (path, forbidden) in new (string Path, string[] Forbidden)[]
                 {
                     (
                         Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalReconciliationWorkbenchService.cs"),
                         new[] { "IOperationalDashboardService", "IOperationalInventoryWorkbenchService", "IOperationalReplayWorkbenchService" }),
                     (
                         Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalInventoryWorkbenchService.cs"),
                         new[] { "IOperationalDashboardService", "IOperationalReconciliationWorkbenchService", "IOperationalReplayWorkbenchService" }),
                     (
                         Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalReplayWorkbenchService.cs"),
                         new[] { "IOperationalDashboardService", "IOperationalReconciliationWorkbenchService", "IOperationalInventoryWorkbenchService" })
                 })
        {
            var text = File.ReadAllText(path);
            Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Dashboard_service_uses_composition_hub()
    {
        var text = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDashboardService.cs"));
        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalResilienceDiagnosticsService", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_composition_layer_does_not_reference_webapi_or_governance_pipeline()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalComposition");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tannous.Pos.WebApi", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceProjectionPipeline", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceSnapshotBuilder", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExplainabilityComposer", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Composition_hub_has_no_parallel_orchestration_or_background_workers()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalComposition",
            "OperationalReadCompositionHub.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Composition_hub_is_registered_as_scoped_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalReadCompositionHub", program, StringComparison.Ordinal);
        Assert.Contains("OperationalReadCompositionHub", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Composition_telemetry_fields_exist_on_cache_snapshot()
    {
        var text = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "Audit",
            "OperationalDiagnosticsCacheTelemetrySnapshotDto.cs"));
        Assert.Contains("CompositionReuseHits", text, StringComparison.Ordinal);
        Assert.Contains("CompositionReuseMisses", text, StringComparison.Ordinal);
        Assert.Contains("CompositionNestedReadAvoidanceCount", text, StringComparison.Ordinal);
        Assert.Contains("CompositionSnapshotBuilds", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Governance_overview_exposes_additive_composition_fields()
    {
        var text = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "Audit",
            "OperationalCacheGovernanceOverviewDto.cs"));
        Assert.Contains("CompositionReuseRatio", text, StringComparison.Ordinal);
        Assert.Contains("NestedCompositionAvoidance", text, StringComparison.Ordinal);
    }
}
