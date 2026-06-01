using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAttentionArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Attention_controller_includes_endpoints_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditAttentionController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/attention", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"priorities\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Attention_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalAttentionService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAttentionSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalResilienceCognitionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Attention_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalAttention");
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
    public void Attention_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalAttention",
            "OperationalAttentionSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalAttentionSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Attention_aggregation_limits_outputs_and_orders_deterministically()
    {
        var report = OperationalAttentionAggregation.ComposeAttentionReport(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalSituationRoomDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalPropagationAnalysisDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalDigestDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto { ConvergenceStrength = OperationalConvergenceStrength.Strong },
            new OperationalResilienceReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalAttentionSnapshot>(),
            new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(report.Priorities.Count <= OperationalAttentionAggregation.MaxPriorities);
        Assert.True(report.AttentionCoordination.Count <= OperationalAttentionAggregation.MaxCoordination);
        Assert.True(report.OperationalEmphasis.Count <= OperationalAttentionAggregation.MaxEmphasis);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatorSummary));
    }

    [Fact]
    public void Attention_interpretation_avoids_automation_and_probabilistic_terminology()
    {
        var report = OperationalAttentionAggregation.ComposeAttentionReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalPropagationAnalysisDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalDigestDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            new OperationalResilienceReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalAttentionSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Notification", "Alerting", "Probabilistic", "MachineLearning", "Workflow" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Attention_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalAttentionService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalAttentionService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalAttentionSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Attention_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalAttentionService.cs"));
        Assert.Contains("Operational attention observability:", service, StringComparison.Ordinal);
    }
}
