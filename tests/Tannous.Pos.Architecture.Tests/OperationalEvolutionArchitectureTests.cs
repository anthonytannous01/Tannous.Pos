using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalEvolutionArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Evolution_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditEvolutionController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/evolution", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"momentum\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditEvolutionController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalEvolution");
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
    public void Evolution_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEvolutionService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalEvolutionSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDigestService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalExperienceGraphService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIntegrityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEvolution",
            "OperationalEvolutionSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalEvolutionSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc);
        var priorDigest = new OperationalDigestSnapshot
        {
            GeneratedAtUtc = generatedAtUtc.AddMinutes(-5),
            DigestState = OperationalDigestState.Escalating,
            DominantOperationalStory = "Prior story",
            DominantRiskArea = OperationalEvolutionAggregation.AreaReplay
        };

        var timeline = OperationalEvolutionAggregation.ComposeEvolutionTimeline(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalEvolutionAggregation.AreaReplay },
            new OperationalSituationRoomDto(),
            new OperationalDigestDto { DigestState = OperationalDigestState.Recovering },
            new OperationalIntegrityReportDto(),
            new OperationalPatternSummaryDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalExperienceGraphDto(),
            new[] { priorDigest },
            Array.Empty<OperationalIntegritySnapshot>(),
            Array.Empty<OperationalSituationSnapshot>(),
            Array.Empty<OperationalPatternSnapshot>(),
            Array.Empty<OperationalExperienceSnapshot>(),
            Array.Empty<OperationalSimulationSnapshot>(),
            Array.Empty<OperationalEvolutionSnapshot>(),
            generatedAtUtc);

        Assert.True(timeline.Transitions.Count <= OperationalEvolutionAggregation.MaxTransitions);
        Assert.True(timeline.Phases.Count <= OperationalEvolutionAggregation.MaxPhases);
        Assert.False(string.IsNullOrWhiteSpace(timeline.OperatorSummary));
    }

    [Fact]
    public void Evolution_interpretation_uses_operator_wording_only()
    {
        var timeline = OperationalEvolutionAggregation.ComposeEvolutionTimeline(
            new OperationalTrendSummaryDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalSituationRoomDto(),
            new OperationalDigestDto(),
            new OperationalIntegrityReportDto(),
            new OperationalPatternSummaryDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalExperienceGraphDto(),
            Array.Empty<OperationalDigestSnapshot>(),
            Array.Empty<OperationalIntegritySnapshot>(),
            Array.Empty<OperationalSituationSnapshot>(),
            Array.Empty<OperationalPatternSnapshot>(),
            Array.Empty<OperationalExperienceSnapshot>(),
            Array.Empty<OperationalSimulationSnapshot>(),
            Array.Empty<OperationalEvolutionSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "LLM", "MachineLearning", "Forecast", "TimeSeries" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, timeline.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.All(timeline.Transitions, t => Assert.DoesNotContain(term, t.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Evolution_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalEvolutionService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalEvolutionService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalEvolutionSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEvolutionService.cs"));
        Assert.Contains("Operational evolution observability:", service, StringComparison.Ordinal);
    }
}
