using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalPatternArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Pattern_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditPatternController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/patterns", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"archetypes\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pattern_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditPatternController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pattern_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalPatterns");
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
    public void Pattern_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPatternService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalPatternSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalPlaybookService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSimulationService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSituationRoomService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pattern_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPatterns",
            "OperationalPatternSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalPatternSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pattern_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 18, 0, 0, DateTimeKind.Utc);
        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalPatternAggregation.AreaReplay,
                    TargetArea = OperationalPatternAggregation.AreaReconciliation,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var patterns = OperationalPatternAggregation.ComposePatterns(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalPatternAggregation.AreaReplay },
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            new OperationalPlaybooksDto(),
            Array.Empty<OperationalPatternSnapshot>(),
            generatedAtUtc);

        Assert.True(patterns.Patterns.Count <= OperationalPatternAggregation.MaxPatterns);
        Assert.True(patterns.Correlations.Count <= OperationalPatternAggregation.MaxCorrelations);
        Assert.True(patterns.Sequences.Count <= OperationalPatternAggregation.MaxSequences);

        var repeat = OperationalPatternAggregation.ComposePatterns(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalPatternAggregation.AreaReplay },
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            new OperationalPlaybooksDto(),
            Array.Empty<OperationalPatternSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            patterns.Patterns.Select(p => p.PatternId),
            repeat.Patterns.Select(p => p.PatternId));
    }

    [Fact]
    public void Pattern_interpretation_uses_operator_wording_only()
    {
        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalPatternAggregation.AreaReplay,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var patterns = OperationalPatternAggregation.ComposePatterns(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            propagation,
            new OperationalCausalChainsDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            new OperationalPlaybooksDto(),
            Array.Empty<OperationalPatternSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph", "LLM", "MachineLearning", "Anomaly" };
        foreach (var term in forbidden)
        {
            Assert.All(patterns.Patterns, p => Assert.DoesNotContain(term, p.OperatorSummary, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Pattern_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalPatternService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalPatternService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalPatternSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Pattern_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPatternService.cs"));
        Assert.Contains("Operational pattern observability:", service, StringComparison.Ordinal);
    }
}
