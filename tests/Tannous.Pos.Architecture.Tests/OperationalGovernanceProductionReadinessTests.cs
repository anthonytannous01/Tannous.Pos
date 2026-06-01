using System.Text.RegularExpressions;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceProductionReadinessTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static int CountFiles(string relativeDir, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), relativeDir), pattern, SearchOption.AllDirectories).Count();

    [Fact]
    public void Measured_governance_ceilings_remain_within_budget()
    {
        var endpointCount = Regex.Matches(
            Read(
                "Tannous.Pos.WebApi",
                "Controllers",
                "Internal",
                "OperationalAuditCacheDiagnosticsController.cs"),
            @"\[HttpGet\(""").Count;

        var collaboratorCount = CountFiles(
            "Tannous.Pos.Infrastructure\\Services\\OperationalDiagnosticsProjections",
            "*Collaborator*.cs");

        var projectionBuilderCount = CountFiles("Tannous.Pos.Application\\Audit", "*ProjectionBuilder*.cs");
        var classifierCount = CountFiles("Tannous.Pos.Application\\Audit", "*Classifier*.cs");
        var explainabilityBuilderCount = CountFiles("Tannous.Pos.Application\\Audit", "*ExplainabilityBuilder*.cs");
        var dtoCount = CountFiles("Tannous.Pos.Application\\Audit", "*Dto.cs");

        var snapshot = OperationalGovernanceCeilingMeasurement.Measure(
            endpointCount,
            collaboratorCount,
            projectionBuilderCount,
            classifierCount,
            explainabilityBuilderCount,
            dtoCount);

        Assert.True(
            snapshot.IsWithinBudget(),
            $"Governance ceilings exceeded: endpoints={snapshot.CacheDiagnosticsGetEndpointCount}, "
            + $"collaborators={snapshot.ProjectionCollaboratorCount}, "
            + $"pipelineStages={snapshot.PipelineStageCount}, "
            + $"builders={snapshot.GovernanceProjectionBuilderCount}, "
            + $"classifiers={snapshot.GovernanceClassifierCount}, "
            + $"explainabilityBuilders={snapshot.GovernanceExplainabilityBuilderCount}, "
            + $"dtos={snapshot.GovernanceDiagnosticsDtoCount}, "
            + $"explainabilityContributors={snapshot.ExplainabilityContributorCount}");
    }

    [Fact]
    public void Runtime_baseline_is_exposed_via_runtime_protection_without_new_endpoint()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var runtimeProtection = Read(
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceRuntimeProtectionDto.cs");
        var collaborator = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator.cs");

        Assert.Contains("[HttpGet(\"runtime-protection\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpGet(\"runtime-baseline\")]", controller, StringComparison.Ordinal);
        Assert.Contains("RuntimeBaseline", runtimeProtection, StringComparison.Ordinal);
        Assert.Contains("Operational governance runtime baseline:", collaborator, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_memoizer_is_request_scoped_without_static_state()
    {
        var memoizer = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceProjectionMemoizer.cs");

        Assert.Contains("OperationalGovernanceProjectionMemoizer", memoizer, StringComparison.Ordinal);
        Assert.DoesNotContain("static readonly", memoizer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("static class", memoizer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IMemoryCache", memoizer, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", memoizer, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_readiness_classifier_is_advisory_only()
    {
        var classifier = Read(
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceProductionReadinessClassifier.cs");

        Assert.Contains("advisory only", classifier, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SaveChangesAsync", classifier, StringComparison.Ordinal);
    }

    [Fact]
    public void Endpoint_count_remains_within_surface_budget()
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
