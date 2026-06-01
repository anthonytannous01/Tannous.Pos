using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalSimulationArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Simulation_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditSimulationController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/simulation", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"outlook\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulation_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditSimulationController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulation_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalSimulation");
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
    public void Simulation_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSimulationService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalSimulationSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSituationRoomService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulation_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSimulation",
            "OperationalSimulationSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalSimulationSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulation_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 16, 0, 0, DateTimeKind.Utc);
        var recovery = new OperationalRecoveryPostureDto
        {
            OverallState = OperationalRecoveryState.Degrading,
            OverallDirection = OperationalRecoveryDirection.Diverging,
            OverallConfidence = OperationalRecoveryConfidence.Low
        };

        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalSimulationAggregation.AreaReplay,
                    TargetArea = OperationalSimulationAggregation.AreaReconciliation,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var scenarios = OperationalSimulationAggregation.ComposeScenarios(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            recovery,
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto { ActiveIncidentCount = 1 },
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalSimulationAggregation.AreaReplay },
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto
            {
                StabilizationDirection = OperationalSituationDirection.Degrading,
                OutlookDetail = new OperationalSituationOutlookDto { DominantConstraint = "Replay pressure" }
            },
            Array.Empty<OperationalSimulationSnapshot>(),
            generatedAtUtc);

        Assert.True(scenarios.Scenarios.Count <= OperationalSimulationAggregation.MaxScenarios);
        Assert.True(scenarios.StabilizationPaths.Count <= OperationalSimulationAggregation.MaxStabilizationPaths);
        Assert.True(scenarios.DegradationPaths.Count <= OperationalSimulationAggregation.MaxDegradationPaths);
        Assert.True(scenarios.LeveragePoints.Count <= OperationalSimulationAggregation.MaxLeveragePoints);

        var repeat = OperationalSimulationAggregation.ComposeScenarios(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            recovery,
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto { ActiveIncidentCount = 1 },
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalSimulationAggregation.AreaReplay },
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto
            {
                StabilizationDirection = OperationalSituationDirection.Degrading,
                OutlookDetail = new OperationalSituationOutlookDto { DominantConstraint = "Replay pressure" }
            },
            Array.Empty<OperationalSimulationSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            scenarios.Scenarios.Select(s => s.ScenarioId),
            repeat.Scenarios.Select(s => s.ScenarioId));
    }

    [Fact]
    public void Simulation_interpretation_uses_operator_wording_only()
    {
        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalSimulationAggregation.AreaReplay,
                    TargetArea = OperationalSimulationAggregation.AreaReconciliation,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var scenarios = OperationalSimulationAggregation.ComposeScenarios(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto(),
            Array.Empty<OperationalSimulationSnapshot>(),
            DateTime.UtcNow);

        var summary = OperationalSimulationAggregation.ComposeSummary(
            scenarios,
            new OperationalSituationRoomDto(),
            new OperationalRecoveryPostureDto(),
            propagation,
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph", "LLM", "Probabilistic" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(scenarios.Scenarios, s => Assert.DoesNotContain(term, s.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Simulation_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalSimulationService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalSimulationService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalSimulationSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulation_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSimulationService.cs"));
        Assert.Contains("Operational simulation observability:", service, StringComparison.Ordinal);
    }
}
