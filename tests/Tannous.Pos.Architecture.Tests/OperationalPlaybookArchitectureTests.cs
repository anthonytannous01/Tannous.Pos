using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTriage;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalPlaybookArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Playbook_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditPlaybookController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/playbooks", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"stabilization-guidance\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Playbook_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditPlaybookController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Playbook_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalPlaybooks");
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
    public void Playbook_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPlaybookService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalPlaybookSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSimulationService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalSituationRoomService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalCausalityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Playbook_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPlaybooks",
            "OperationalPlaybookSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalPlaybookSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Playbook_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 17, 0, 0, DateTimeKind.Utc);
        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalPlaybookAggregation.AreaReplay,
                    TargetArea = OperationalPlaybookAggregation.AreaReconciliation,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var playbooks = OperationalPlaybookAggregation.ComposePlaybooks(
            new OperationalDashboardSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto { OverallConfidence = OperationalRecoveryConfidence.Low },
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            propagation,
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            Array.Empty<OperationalPlaybookSnapshot>(),
            generatedAtUtc);

        Assert.True(playbooks.Playbooks.Count <= OperationalPlaybookAggregation.MaxPlaybooks);
        Assert.True(playbooks.ResponseSteps.Count <= OperationalPlaybookAggregation.MaxResponseSteps);
        Assert.True(playbooks.EscalationGuidance.Count <= OperationalPlaybookAggregation.MaxEscalationGuidance);

        var repeat = OperationalPlaybookAggregation.ComposePlaybooks(
            new OperationalDashboardSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto { OverallConfidence = OperationalRecoveryConfidence.Low },
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            propagation,
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            Array.Empty<OperationalPlaybookSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            playbooks.Playbooks.Select(p => p.PlaybookId),
            repeat.Playbooks.Select(p => p.PlaybookId));
    }

    [Fact]
    public void Playbook_guidance_uses_operator_wording_only()
    {
        var propagation = new OperationalPropagationAnalysisDto
        {
            Propagations = new[]
            {
                new OperationalPressurePropagationDto
                {
                    SourceArea = OperationalPlaybookAggregation.AreaReplay,
                    IsEscalating = true,
                    OperatorInterpretation = "Replay pressure propagating toward reconciliation visibility"
                }
            }
        };

        var playbooks = OperationalPlaybookAggregation.ComposePlaybooks(
            new OperationalDashboardSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalRecoveryOutlookDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            propagation,
            new OperationalSituationRoomDto(),
            new OperationalSimulationScenariosDto(),
            Array.Empty<OperationalPlaybookSnapshot>(),
            DateTime.UtcNow);

        var guidance = OperationalPlaybookAggregation.ComposeStabilizationGuidance(
            playbooks,
            new OperationalSimulationScenariosDto(),
            propagation,
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto());

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph", "LLM", "Workflow", "Automation" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, guidance.OperatorPriority, StringComparison.OrdinalIgnoreCase);
            Assert.All(playbooks.Playbooks, p => Assert.DoesNotContain(term, p.OperatorSummary, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Playbook_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalPlaybookService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalPlaybookService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalPlaybookSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Playbook_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalPlaybookService.cs"));
        Assert.Contains("Operational playbook observability:", service, StringComparison.Ordinal);
    }
}
