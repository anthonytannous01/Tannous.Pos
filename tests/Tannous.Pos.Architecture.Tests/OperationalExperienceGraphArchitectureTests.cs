using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalExperienceGraphArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Experience_graph_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditExperienceGraphController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/experience-graph", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"traversal\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"navigation\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Experience_graph_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditExperienceGraphController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Experience_graph_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalExperienceGraph");
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
    public void Experience_graph_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalExperienceGraphService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalExperienceSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIntegrityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalPatternService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalPlaybookService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSimulationService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSituationRoomService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Experience_graph_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalExperienceGraph",
            "OperationalExperienceSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalExperienceSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Experience_graph_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 22, 0, 0, DateTimeKind.Utc);
        var causalitySummary = new OperationalCausalitySummaryDto
        {
            DominantOperationalArea = OperationalExperienceGraphAggregation.AreaReplay
        };

        var graph = OperationalExperienceGraphAggregation.ComposeExperienceGraph(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto { HighestLeverageArea = OperationalExperienceGraphAggregation.AreaReplay },
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalExperienceSnapshot>(),
            generatedAtUtc);

        Assert.True(graph.Relationships.Count <= OperationalExperienceGraphAggregation.MaxRelationships);
        Assert.False(string.IsNullOrWhiteSpace(graph.RecommendedEntryPoint));

        var repeat = OperationalExperienceGraphAggregation.ComposeExperienceGraph(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto { HighestLeverageArea = OperationalExperienceGraphAggregation.AreaReplay },
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalExperienceSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            graph.Relationships.Select(r => r.SourceSurface + r.TargetSurface + r.RelationshipType),
            repeat.Relationships.Select(r => r.SourceSurface + r.TargetSurface + r.RelationshipType));
    }

    [Fact]
    public void Experience_graph_interpretation_uses_operator_wording_only()
    {
        var graph = OperationalExperienceGraphAggregation.ComposeExperienceGraph(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalExperienceGraphAggregation.AreaReplay },
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalExperienceSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "GraphDatabase", "LLM", "MachineLearning", "WorkflowEngine" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, graph.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.All(graph.Relationships, r => Assert.DoesNotContain(term, r.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Experience_graph_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalExperienceGraphService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalExperienceGraphService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalExperienceSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Experience_graph_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalExperienceGraphService.cs"));
        Assert.Contains("Operational experience graph observability:", service, StringComparison.Ordinal);
    }
}
