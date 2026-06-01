using System.Text.RegularExpressions;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.Audit.Governance.Modules;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceModularizationTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static string ReadProjectionBuilder(string fileName) =>
        Read("Tannous.Pos.Application", "Audit", fileName);

    [Fact]
    public void Governance_modules_are_registered_without_cycles()
    {
        var graph = OperationalGovernanceModuleRegistry.DependencyGraph();
        Assert.Equal(6, OperationalGovernanceModuleRegistry.All.Count);
        Assert.False(OperationalGovernanceDependencyRules.HasCircularDependencies(graph));
    }

    [Fact]
    public void Projection_pipeline_has_deterministic_stage_order()
    {
        var stages = OperationalGovernanceProjectionPipeline.StageOrder;
        Assert.Equal(8, stages.Count);
        Assert.Equal("TelemetrySnapshot", stages[0]);
        Assert.Equal("RuntimeProtection", stages[^1]);
        Assert.Equal(stages.Count, stages.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Composition_context_builder_delegates_to_pipeline()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceCompositionContextBuilder.cs");
        Assert.Contains("OperationalGovernanceProjectionPipeline.Execute", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_context_factory_uses_telemetry_access_abstraction()
    {
        var factory = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalDiagnosticsCacheProjectionContextFactory.cs");
        var memoizer = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceProjectionMemoizer.cs");

        Assert.Contains("OperationalGovernanceProjectionMemoizer", factory, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceTelemetryAccess.CaptureSnapshot", memoizer, StringComparison.Ordinal);
        Assert.DoesNotContain("_telemetry.GetSnapshot()", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void Domain_projection_builders_avoid_forbidden_cross_module_classifiers()
    {
        foreach (var (domain, forbidden) in OperationalGovernanceDependencyRules.ForbiddenCrossModuleClassifierReferences)
        {
            var module = OperationalGovernanceModuleRegistry.Get(domain);
            foreach (var builderFile in module.ProjectionBuilderTypes)
            {
                var path = Path.Combine(
                    RepoRoot(),
                    "Tannous.Pos.Application",
                    "Audit",
                    $"{builderFile}.cs");
                if (!File.Exists(path))
                    continue;

                var text = File.ReadAllText(path);
                foreach (var forbiddenType in forbidden)
                {
                    Assert.DoesNotContain(forbiddenType, text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Governance_complexity_remains_within_budget()
    {
        var projectionsDir = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections");
        var collaboratorCount = Directory.Exists(projectionsDir)
            ? Directory.EnumerateFiles(projectionsDir, "*Collaborator*.cs").Count()
            : 0;

        var thresholdEvaluatorUsage = Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit"),
                "*Classifier*.cs",
                SearchOption.TopDirectoryOnly)
            .Count(f => File.ReadAllText(f).Contains("OperationalGovernanceThresholdEvaluator", StringComparison.Ordinal));

        var measurement = OperationalGovernanceComplexityMetrics.Measure(
            collaboratorCount,
            thresholdEvaluatorUsage);

        Assert.True(
            measurement.IsWithinBudget(),
            $"Complexity budget exceeded: stages={measurement.ProjectionPipelineStageCount}, "
            + $"collaborators={measurement.CollaboratorFanout}, coupling={measurement.ModuleCouplingScore}");
    }

    [Fact]
    public void Governance_conventions_document_onboarding_rules()
    {
        Assert.NotEmpty(OperationalGovernanceConventions.NamingStandards);
        Assert.NotEmpty(OperationalGovernanceConventions.AllowedDependencyDirections);
        Assert.Contains("No runtime plugin systems", OperationalGovernanceConventions.NonGoals[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Modularization_does_not_add_runtime_discovery_or_mutation_endpoints()
    {
        var auditGlob = string.Join(
            '\n',
            Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "Governance"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Assembly.Load", auditGlob, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator.CreateInstance", auditGlob, StringComparison.Ordinal);
        Assert.DoesNotContain("Type.GetType", auditGlob, StringComparison.Ordinal);

        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var endpointCount = Regex.Matches(controller, @"\[HttpGet\(""").Count;
        Assert.True(endpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }

    [Fact]
    public void Standard_governance_profile_preserves_default_explainability_cap()
    {
        Assert.Equal(8, OperationalGovernanceProfileSettings.GetExplainabilityCap(OperationalGovernanceProfile.Standard));
        Assert.Equal(
            OperationalGovernanceProfile.Standard,
            OperationalGovernanceProfileSettings.Default);
    }
}
