using System.Text.RegularExpressions;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.Audit.Governance.Modules;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceRuntimeSafeguardTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    [Fact]
    public void Runtime_protection_endpoints_are_get_only()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"runtime-protection\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"execution-diagnostics\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"telemetry-saturation\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_budget_enforces_static_ceilings()
    {
        Assert.Equal(8, OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);
        Assert.Equal(8, OperationalGovernanceRuntimeBudget.MaxGovernanceRecommendations);
        Assert.Equal(8, OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators);
        Assert.Equal(8, OperationalGovernanceRuntimeBudget.MaxPipelineDepth);
        Assert.Equal(12, OperationalGovernanceRuntimeBudget.MaxTelemetryCategories);
        Assert.True(OperationalGovernanceRuntimeBudget.MaxPipelineDepth >= OperationalGovernanceProjectionPipeline.StageOrder.Count);
    }

    [Fact]
    public void Runtime_budget_clamp_helpers_use_deterministic_ordering()
    {
        var items = new[] { "Zeta", "Alpha", "Alpha", "Beta", "  " };
        var clamped = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(items, 3);

        Assert.Equal(["Alpha", "Beta", "Zeta"], clamped);
        Assert.Equal(3, clamped.Count);
    }

    [Fact]
    public void Pipeline_depth_ceiling_matches_runtime_budget()
    {
        Assert.True(
            OperationalGovernanceProjectionPipeline.StageOrder.Count <= OperationalGovernanceRuntimeBudget.MaxPipelineDepth);
        Assert.True(
            OperationalGovernanceProjectionPipeline.StageOrder.Count <= OperationalGovernanceComplexityMetrics.MaxPipelineStageCount);
        Assert.Equal("RuntimeProtection", OperationalGovernanceProjectionPipeline.StageOrder[^1]);
    }

    [Fact]
    public void Module_dependency_graph_remains_acyclic_with_runtime_stage()
    {
        var graph = OperationalGovernanceModuleRegistry.DependencyGraph();
        Assert.False(OperationalGovernanceDependencyRules.HasCircularDependencies(graph));
    }

    [Fact]
    public void Runtime_builders_and_classifiers_are_internal_governance_only()
    {
        var governanceDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "Governance");
        var runtimeFiles = Directory.EnumerateFiles(governanceDir, "OperationalGovernanceRuntime*.cs")
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernance*Failsafe*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceBudgetPressure*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceProjectionComplexity*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceTelemetrySaturation*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceExecutionDiagnostics*.cs"))
            .ToList();

        Assert.NotEmpty(runtimeFiles);

        foreach (var file in runtimeFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Assembly.Load", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IDistributedCache", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Collaborator_fanout_remains_within_budget()
    {
        var projectionsDir = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections");
        var collaboratorCount = Directory.EnumerateFiles(projectionsDir, "*Collaborator*.cs").Count();

        Assert.True(collaboratorCount <= OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators);
        Assert.True(collaboratorCount <= OperationalGovernanceComplexityMetrics.MaxCollaboratorFanout);
    }

    [Fact]
    public void Diagnostics_service_wires_runtime_protection_collaborator()
    {
        var service = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");

        Assert.Contains("OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("GetRuntimeProtectionAsync", service, StringComparison.Ordinal);
        Assert.Contains("GetExecutionDiagnosticsAsync", service, StringComparison.Ordinal);
        Assert.Contains("GetTelemetrySaturationAsync", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Standard_profile_explainability_cap_remains_eight()
    {
        Assert.Equal(
            8,
            OperationalGovernanceProfileSettings.GetExplainabilityCap(OperationalGovernanceProfile.Standard));
        Assert.Equal(
            8,
            OperationalGovernanceRuntimeBudget.GetEffectiveExplainabilityCap(
                OperationalGovernanceExecutionState.Healthy,
                OperationalGovernanceProfile.Standard));
    }

    [Fact]
    public void Cache_diagnostics_endpoint_count_remains_within_surface_budget()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var endpointCount = Regex.Matches(controller, @"\[HttpGet\(""").Count;

        Assert.True(endpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }
}
