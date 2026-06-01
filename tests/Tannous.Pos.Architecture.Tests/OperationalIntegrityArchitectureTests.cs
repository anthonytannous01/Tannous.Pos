using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalIntegrityArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Integrity_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditIntegrityController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/integrity", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"contradictions\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Integrity_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditIntegrityController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Integrity_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalIntegrity");
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
    public void Integrity_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIntegrityService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalIntegritySnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalPatternService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalPlaybookService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSimulationService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSituationRoomService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Integrity_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIntegrity",
            "OperationalIntegritySnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalIntegritySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Integrity_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 20, 0, 0, DateTimeKind.Utc);
        var causalitySummary = new OperationalCausalitySummaryDto
        {
            DominantOperationalArea = OperationalIntegrityAggregation.AreaReplay
        };

        var simulationSummary = new OperationalSimulationSummaryDto
        {
            HighestLeverageArea = OperationalIntegrityAggregation.AreaReplay
        };

        var playbooks = new OperationalPlaybooksDto
        {
            Playbooks = new[]
            {
                new OperationalPlaybookDto
                {
                    PlaybookId = OperationalPlaybookAggregation.PlaybookReplayStabilization,
                    DominantArea = OperationalIntegrityAggregation.AreaReplay
                }
            }
        };

        var report = OperationalIntegrityAggregation.ComposeIntegrityReport(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            simulationSummary,
            new OperationalSimulationOutlookDto(),
            playbooks,
            new OperationalPatternsDto(),
            new OperationalPatternSummaryDto(),
            Array.Empty<OperationalIntegritySnapshot>(),
            generatedAtUtc);

        Assert.True(report.Alignments.Count <= OperationalIntegrityAggregation.MaxAlignments);
        Assert.True(report.IntegrityWarnings.Count <= OperationalIntegrityAggregation.MaxWarnings);
        Assert.InRange(report.ConsistencyScore, 0, 100);

        var repeat = OperationalIntegrityAggregation.ComposeIntegrityReport(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            simulationSummary,
            new OperationalSimulationOutlookDto(),
            playbooks,
            new OperationalPatternsDto(),
            new OperationalPatternSummaryDto(),
            Array.Empty<OperationalIntegritySnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            report.Alignments.Select(a => a.SourceLayer + a.TargetLayer + a.AlignmentType),
            repeat.Alignments.Select(a => a.SourceLayer + a.TargetLayer + a.AlignmentType));
    }

    [Fact]
    public void Integrity_interpretation_uses_operator_wording_only()
    {
        var report = OperationalIntegrityAggregation.ComposeIntegrityReport(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalIntegrityAggregation.AreaReplay },
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            new OperationalSimulationSummaryDto { HighestLeverageArea = OperationalIntegrityAggregation.AreaReplay },
            new OperationalSimulationOutlookDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternsDto(),
            new OperationalPatternSummaryDto(),
            Array.Empty<OperationalIntegritySnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph", "LLM", "MachineLearning", "Probabilistic" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.All(report.Alignments, a => Assert.DoesNotContain(term, a.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Integrity_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalIntegrityService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalIntegrityService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalIntegritySnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Integrity_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIntegrityService.cs"));
        Assert.Contains("Operational integrity observability:", service, StringComparison.Ordinal);
    }
}
