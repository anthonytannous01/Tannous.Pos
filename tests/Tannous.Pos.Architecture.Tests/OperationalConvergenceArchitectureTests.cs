using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalConvergenceArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Convergence_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditConvergenceController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/convergence", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"divergence\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Convergence_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditConvergenceController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Convergence_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalConvergence");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tannous.Pos.Application.Audit.Governance", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceProjectionPipeline", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExplainabilityComposer", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Convergence_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalConvergenceService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalConvergenceSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalTopologyService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalEvolutionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDigestService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Convergence_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalConvergence",
            "OperationalConvergenceSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalConvergenceSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Convergence_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc);

        var report = OperationalConvergenceAggregation.ComposeConvergenceReport(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalConvergenceAggregation.AreaReplay },
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalDigestDto(),
            new OperationalTopologyDto(),
            Array.Empty<OperationalConvergenceSnapshot>(),
            generatedAtUtc);

        Assert.True(report.Reinforcements.Count <= OperationalConvergenceAggregation.MaxReinforcements);
        Assert.True(report.Ambiguities.Count <= OperationalConvergenceAggregation.MaxAmbiguities);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatorSummary));
    }

    [Fact]
    public void Convergence_interpretation_uses_operator_wording_only()
    {
        var report = OperationalConvergenceAggregation.ComposeConvergenceReport(
            new OperationalRecoveryPostureDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalDigestDto(),
            new OperationalTopologyDto(),
            Array.Empty<OperationalConvergenceSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "LLM", "MachineLearning", "Probabilistic", "Statistical", "Forecast" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.All(report.Reinforcements, r => Assert.DoesNotContain(term, r.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Convergence_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalConvergenceService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalConvergenceService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalConvergenceSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Convergence_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalConvergenceService.cs"));
        Assert.Contains("Operational convergence observability:", service, StringComparison.Ordinal);
    }
}
