using System.Text.RegularExpressions;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceConsolidationTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static int CountAuditFiles(string pattern) =>
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit"),
                pattern,
                SearchOption.TopDirectoryOnly)
            .Count();

    [Fact]
    public void Governance_surface_remains_within_budget()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        var endpointCount = Regex.Matches(controller, @"\[HttpGet\(""").Count;
        var projectionBuilderCount = CountAuditFiles("*ProjectionBuilder*.cs");
        var explainabilityBuilderCount = CountAuditFiles("*ExplainabilityBuilder*.cs");
        var classifierCount = CountAuditFiles("*Classifier*.cs");
        var dtoCount = CountAuditFiles("*Dto.cs");

        var measurement = Tannous.Pos.Application.Audit.OperationalGovernanceSurfaceBudget.MeasureFromSources(
            endpointCount,
            projectionBuilderCount,
            explainabilityBuilderCount,
            classifierCount,
            dtoCount);

        Assert.True(
            measurement.IsWithinBudget(),
            $"Governance surface exceeded budget: endpoints={measurement.CacheDiagnosticsGetEndpointCount}, "
            + $"projectionBuilders={measurement.GovernanceProjectionBuilderCount}, "
            + $"explainabilityBuilders={measurement.GovernanceExplainabilityBuilderCount}, "
            + $"classifiers={measurement.GovernanceClassifierCount}, "
            + $"dtos={measurement.GovernanceDiagnosticsDtoCount}");
    }

    [Fact]
    public void Explainability_builders_delegate_to_composer()
    {
        var cache = Read("Tannous.Pos.Application", "Audit", "OperationalCacheExplainabilityBuilder.cs");
        var consistency = Read("Tannous.Pos.Application", "Audit", "OperationalCacheConsistencyExplainabilityBuilder.cs");
        var invalidation = Read("Tannous.Pos.Application", "Audit", "OperationalCacheInvalidationExplainabilityBuilder.cs");
        var pressure = Read("Tannous.Pos.Application", "Audit", "OperationalPressureExplainabilityBuilder.cs");

        Assert.Contains("OperationalGovernanceExplainabilityComposer.Compose", cache, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceExplainabilityComposer.Compose", consistency, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceExplainabilityComposer.Compose", invalidation, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceExplainabilityComposer.Compose", pressure, StringComparison.Ordinal);
    }

    [Fact]
    public void Composition_context_builder_exists_for_projection_reuse()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceCompositionContextBuilder.cs");
        Assert.Contains("OperationalGovernanceCompositionContext", text, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceProjectionPipeline.Execute", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IMemoryCache", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagnostics_service_uses_projection_collaborators()
    {
        var service = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");

        Assert.Contains("OperationalDiagnosticsCacheGovernanceProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheInvalidationProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCachePressureProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheConsistencyProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheSurvivabilityProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceSnapshotProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceFingerprintProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceProjectionMemoizer", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Threshold_evaluator_is_used_by_classifiers()
    {
        var stability = Read("Tannous.Pos.Application", "Audit", "OperationalCacheStabilityClassifier.cs");
        var confidence = Read("Tannous.Pos.Application", "Audit", "OperationalCacheConsistencyConfidenceClassifier.cs");
        var convergence = Read("Tannous.Pos.Application", "Audit", "OperationalPressureConvergenceClassifier.cs");

        Assert.Contains("OperationalGovernanceThresholdEvaluator", stability, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceThresholdEvaluator", confidence, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceThresholdEvaluator", convergence, StringComparison.Ordinal);
    }

    [Fact]
    public void Consolidation_does_not_add_mutation_endpoints_or_persistence()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var composer = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceExplainabilityComposer.cs");

        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Explainability_composer_enforces_bounded_limits()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceExplainabilityComposer.cs");
        Assert.Contains("Distinct(StringComparer.Ordinal)", text, StringComparison.Ordinal);
        Assert.Contains(".Take(maxItems)", text, StringComparison.Ordinal);
        Assert.Contains("ExplainabilityProfile", text, StringComparison.Ordinal);
    }
}
