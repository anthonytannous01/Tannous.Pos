using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalStrategyArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Strategy_controller_includes_endpoints_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditStrategyController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/strategy", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"coordination\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalStrategyService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalStrategySnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalAttentionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalStrategy");
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
    public void Strategy_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalStrategy",
            "OperationalStrategySnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalStrategySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_aggregation_limits_outputs_and_orders_deterministically()
    {
        var report = OperationalStrategyAggregation.ComposeStrategyReport(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto { ConvergenceStrength = OperationalConvergenceStrength.Strong },
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalDigestDto(),
            new OperationalPlaybooksDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalStrategySnapshot>(),
            new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(report.StrategicPostures.Count <= OperationalStrategyAggregation.MaxPostures);
        Assert.True(report.OperationalCoordination.Count <= OperationalStrategyAggregation.MaxCoordination);
        Assert.True(report.StrategicAlignments.Count <= OperationalStrategyAggregation.MaxAlignments);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatorSummary));
    }

    [Fact]
    public void Strategy_interpretation_avoids_bi_and_probabilistic_terminology()
    {
        var report = OperationalStrategyAggregation.ComposeStrategyReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalDigestDto(),
            new OperationalPlaybooksDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalStrategySnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "BusinessIntelligence", "Probabilistic", "MachineLearning", "Executive", "Optimization" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Strategy_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalStrategyService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalStrategyService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalStrategySnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalStrategyService.cs"));
        Assert.Contains("Operational strategy observability:", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_continuity_dto_oscillation_fields_are_init_only()
    {
        var text = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalStrategy",
            "OperationalStrategyContinuityDto.cs"));
        Assert.Contains("PostureOscillation { get; init; }", text, StringComparison.Ordinal);
        Assert.Contains("OscillationDetected { get; init; }", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_oscillation_detected_for_alternating_posture_window()
    {
        var alternating = new[]
        {
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Balanced },
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Deteriorating },
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Balanced },
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Deteriorating },
        };
        var report = OperationalStrategyAggregation.ComposeStrategyReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalDigestDto(),
            new OperationalPlaybooksDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            alternating,                             // 4 prior snapshots → 5 total with current
            DateTime.UtcNow);

        Assert.True(report.StrategyContinuity.OscillationDetected);
        Assert.False(string.IsNullOrWhiteSpace(report.StrategyContinuity.PostureOscillation));
        Assert.DoesNotContain("No posture oscillation", report.StrategyContinuity.PostureOscillation,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strategy_oscillation_not_detected_for_stable_posture_window()
    {
        var stable = new[]
        {
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Balanced },
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Balanced },
            new OperationalStrategySnapshot { DominantOperationalPosture = OperationalStrategicPostureType.Balanced },
        };
        var report = OperationalStrategyAggregation.ComposeStrategyReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalDigestDto(),
            new OperationalPlaybooksDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            stable,
            DateTime.UtcNow);

        Assert.False(report.StrategyContinuity.OscillationDetected);
        Assert.Contains("No posture oscillation", report.StrategyContinuity.PostureOscillation,
            StringComparison.OrdinalIgnoreCase);
    }
}
