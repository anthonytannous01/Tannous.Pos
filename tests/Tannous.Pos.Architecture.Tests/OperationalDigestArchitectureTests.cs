using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalExperienceGraph;
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

public class OperationalDigestArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Digest_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditDigestController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/digest", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"executive\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Digest_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditDigestController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Digest_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalDigest");
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
    public void Digest_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDigestService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalDigestSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalExperienceGraphService", text, StringComparison.Ordinal);
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
    public void Digest_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDigest",
            "OperationalDigestSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalDigestSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Digest_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 23, 0, 0, DateTimeKind.Utc);
        var causalitySummary = new OperationalCausalitySummaryDto
        {
            DominantOperationalArea = OperationalDigestAggregation.AreaReplay
        };

        var digest = OperationalDigestAggregation.ComposeOperationalDigest(
            new OperationalTrendSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto { HighestLeverageArea = OperationalDigestAggregation.AreaReplay },
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalContextualNavigationDto(),
            new OperationalExperienceTraversalPathsDto(),
            Array.Empty<OperationalDigestSnapshot>(),
            generatedAtUtc);

        Assert.True(digest.OperationalHighlights.Count <= OperationalDigestAggregation.MaxHighlights);
        Assert.True(digest.NavigationHighlights.Count <= OperationalDigestAggregation.MaxNavigationHighlights);
        Assert.False(string.IsNullOrWhiteSpace(digest.DominantOperationalStory));

        var repeat = OperationalDigestAggregation.ComposeOperationalDigest(
            new OperationalTrendSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            causalitySummary,
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto { HighestLeverageArea = OperationalDigestAggregation.AreaReplay },
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalContextualNavigationDto(),
            new OperationalExperienceTraversalPathsDto(),
            Array.Empty<OperationalDigestSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            digest.OperationalHighlights.Select(h => h.Title),
            repeat.OperationalHighlights.Select(h => h.Title));
    }

    [Fact]
    public void Digest_interpretation_uses_operator_wording_only()
    {
        var digest = OperationalDigestAggregation.ComposeOperationalDigest(
            new OperationalTrendSummaryDto(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalIncidentCasesSummaryDto(),
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalDigestAggregation.AreaReplay },
            new OperationalPropagationAnalysisDto(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalContextualNavigationDto(),
            new OperationalExperienceTraversalPathsDto(),
            Array.Empty<OperationalDigestSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "LLM", "MachineLearning", "Analytics" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, digest.OperatorDigest, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, digest.ExecutiveDigest, StringComparison.OrdinalIgnoreCase);
            Assert.All(digest.OperationalHighlights, h => Assert.DoesNotContain(term, h.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Digest_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalDigestService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalDigestService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalDigestSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Digest_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDigestService.cs"));
        Assert.Contains("Operational digest observability:", service, StringComparison.Ordinal);
    }
}
