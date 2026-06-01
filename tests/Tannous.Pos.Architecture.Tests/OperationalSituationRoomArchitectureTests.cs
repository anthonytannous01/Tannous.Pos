using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalSituationRoomArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Situation_room_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditSituationRoomController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/situation-room", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"briefing\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_room_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditSituationRoomController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_room_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalSituationRoom");
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
    public void Situation_room_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSituationRoomService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalSituationSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_room_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSituationRoom",
            "OperationalSituationSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalSituationSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_room_aggregation_limits_narratives_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 15, 0, 0, DateTimeKind.Utc);
        var recovery = new OperationalRecoveryPostureDto
        {
            OverallState = OperationalRecoveryState.Degrading,
            OverallDirection = OperationalRecoveryDirection.Diverging,
            OverallConfidence = OperationalRecoveryConfidence.Low,
            Summary = "Operational conditions degrading"
        };

        var room = OperationalSituationRoomAggregation.ComposeSituationRoom(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            recovery,
            new OperationalRecoveryOutlookDto { Summary = "Recovery constrained" },
            new OperationalIncidentCasesSummaryDto { ActiveIncidentCount = 1 },
            new OperationalCausalitySummaryDto
            {
                DominantOperationalArea = OperationalSituationRoomAggregation.AreaReplay,
                EscalatingPropagationCount = 2
            },
            new OperationalPropagationAnalysisDto
            {
                Propagations = new[]
                {
                    new OperationalPressurePropagationDto
                    {
                        SourceArea = OperationalSituationRoomAggregation.AreaReplay,
                        IsEscalating = true,
                        OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                    }
                }
            },
            new OperationalCausalChainsDto(),
            Array.Empty<OperationalSituationSnapshot>(),
            generatedAtUtc);

        Assert.True(room.Narratives.Count <= OperationalSituationRoomAggregation.MaxNarratives);
        Assert.True(room.RiskConcentrations.Count <= OperationalSituationRoomAggregation.MaxRiskConcentrations);
        Assert.Contains("Replay", room.DominantOperationalRisk, StringComparison.OrdinalIgnoreCase);

        var repeat = OperationalSituationRoomAggregation.ComposeSituationRoom(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            recovery,
            new OperationalRecoveryOutlookDto { Summary = "Recovery constrained" },
            new OperationalIncidentCasesSummaryDto { ActiveIncidentCount = 1 },
            new OperationalCausalitySummaryDto
            {
                DominantOperationalArea = OperationalSituationRoomAggregation.AreaReplay,
                EscalatingPropagationCount = 2
            },
            new OperationalPropagationAnalysisDto
            {
                Propagations = new[]
                {
                    new OperationalPressurePropagationDto
                    {
                        SourceArea = OperationalSituationRoomAggregation.AreaReplay,
                        IsEscalating = true,
                        OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                    }
                }
            },
            new OperationalCausalChainsDto(),
            Array.Empty<OperationalSituationSnapshot>(),
            generatedAtUtc);

        Assert.Equal(room.DominantOperationalRisk, repeat.DominantOperationalRisk);
        Assert.Equal(room.Narratives.Select(n => n.Title), repeat.Narratives.Select(n => n.Title));
    }

    [Fact]
    public void Situation_room_briefing_uses_operator_wording_only()
    {
        var room = OperationalSituationRoomAggregation.ComposeSituationRoom(
            new OperationalDashboardSummaryDto(),
            new OperationalTrendSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto { Summary = "Recovery improving" },
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalPropagationAnalysisDto(),
            new OperationalCausalChainsDto(),
            Array.Empty<OperationalSituationSnapshot>(),
            DateTime.UtcNow);

        var briefing = OperationalSituationRoomAggregation.ComposeExecutiveBriefing(room);
        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph", "LLM" };

        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, room.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, room.ExecutiveSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, briefing.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(room.Narratives, n => Assert.DoesNotContain(term, n.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Situation_room_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalSituationRoomService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalSituationRoomService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalSituationSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Situation_room_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalSituationRoomService.cs"));
        Assert.Contains("Operational situation room observability:", service, StringComparison.Ordinal);
    }
}
