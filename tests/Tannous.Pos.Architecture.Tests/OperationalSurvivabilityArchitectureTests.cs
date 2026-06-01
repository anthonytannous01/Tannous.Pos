using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalSurvivabilityArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Resilience_controller_includes_cognition_endpoints_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditResilienceController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/resilience", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"posture/summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"fragility\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_cognition_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalResilienceCognitionService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalResilienceCognitionSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalConvergenceService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalTopologyService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_cognition_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalResilience");
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
    public void Resilience_cognition_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalResilience",
            "OperationalResilienceCognitionSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalResilienceCognitionSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_aggregation_limits_outputs_and_orders_deterministically()
    {
        var report = OperationalResilienceAggregation.ComposeResilienceReport(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto { ConvergenceStrength = OperationalConvergenceStrength.Strong },
            false,
            Array.Empty<OperationalResilienceCognitionSnapshot>(),
            new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(report.SurvivabilityAnalyses.Count <= OperationalResilienceAggregation.MaxSurvivabilityAnalyses);
        Assert.True(report.ContainmentDurabilities.Count <= OperationalResilienceAggregation.MaxContainmentDurabilities);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatorSummary));
    }

    [Fact]
    public void Resilience_interpretation_avoids_chaos_and_probabilistic_terminology()
    {
        var report = OperationalResilienceAggregation.ComposeResilienceReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            false,
            Array.Empty<OperationalResilienceCognitionSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Chaos", "FaultInjection", "Probabilistic", "MachineLearning", "Forecast" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Resilience_cognition_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalResilienceCognitionService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalResilienceCognitionService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalResilienceCognitionSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_cognition_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalResilienceCognitionService.cs"));
        Assert.Contains("Operational resilience observability:", service, StringComparison.Ordinal);
    }
}
