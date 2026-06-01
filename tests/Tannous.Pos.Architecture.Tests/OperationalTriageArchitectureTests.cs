using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalTriageArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Triage_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTriageController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/triage", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"recommendations\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Triage_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTriageController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Triage_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalTriage");
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
    public void Triage_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTriageService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalNavigationService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Triage_aggregation_limits_items_and_orders_deterministically()
    {
        var navigation = new OperationalNavigationIndexDto
        {
            Recommendations = new[]
            {
                new OperationalNavigationRecommendationDto
                {
                    Priority = 1,
                    Title = "Critical replay instability",
                    RecommendedAction = "Review replay stabilization workbench",
                    RelativeRoute = OperationalTriageAggregation.RouteReplayWorkbench,
                    Severity = OperationalNavigationSeverity.Critical
                }
            }
        };
        var replayPressure = new OperationalReplayPressureSummaryDto
        {
            InstabilityLevel = OperationalReplayPressureLevel.Critical,
            Summary = "Replay instability requires investigation"
        };
        var replayStabilization = new OperationalReplayStabilizationDto { ReplayPressureEscalating = true };
        var inventory = new OperationalInventoryWorkbenchDto
        {
            DriftSummary = new OperationalInventoryDriftSummaryDto
            {
                DriftSeverity = OperationalInventoryDriftSeverity.Critical,
                Summary = "Inventory drift escalation detected"
            }
        };
        var reconciliation = new OperationalReconciliationWorkbenchDto
        {
            Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 3, Summary = "Backlog escalating" }
        };

        var items = OperationalTriageAggregation.ComposeItems(
            navigation,
            new OperationalTimelineDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalDashboardSummaryDto(),
            replayPressure,
            replayStabilization,
            reconciliation,
            inventory,
            runtimeSaturationIndicated: true);

        Assert.True(items.Count <= OperationalTriageAggregation.MaxTriageItems);
        Assert.Equal(OperationalTriageAggregation.RouteReplayWorkbench, items[0].RecommendedRoute);
        Assert.Equal(
            items.OrderBy(i => i.Priority).ThenBy(i => i.Summary, StringComparer.Ordinal).ToList(),
            items.ToList());
    }

    [Fact]
    public void Triage_routes_use_existing_operational_paths_only()
    {
        var queue = OperationalTriageAggregation.ComposeQueue(
            new OperationalNavigationIndexDto(),
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTrendSummaryDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            runtimeSaturationIndicated: false);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalTriageAggregation.RouteDashboard,
            OperationalTriageAggregation.RouteReconciliationWorkbench,
            OperationalTriageAggregation.RouteInventoryWorkbench,
            OperationalTriageAggregation.RouteReplayWorkbench,
            OperationalTriageAggregation.RouteTrendSummary,
            OperationalTriageAggregation.RouteTimeline
        };

        Assert.All(queue.Items, item => Assert.Contains(item.RecommendedRoute, allowed));
    }

    [Fact]
    public void Triage_correlations_are_bounded()
    {
        var items = OperationalTriageAggregation.ComposeItems(
            new OperationalNavigationIndexDto(),
            new OperationalTimelineDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.Critical },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReconciliationWorkbenchDto
            {
                Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 }
            },
            new OperationalInventoryWorkbenchDto
            {
                DriftSummary = new OperationalInventoryDriftSummaryDto
                {
                    DriftSeverity = OperationalInventoryDriftSeverity.High,
                    TotalInventoryDriftConflicts = 3
                }
            },
            runtimeSaturationIndicated: true);

        var correlations = OperationalTriageAggregation.ComposeCorrelations(
            items,
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.Critical },
            new OperationalReplayStabilizationDto(),
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            runtimeSaturationIndicated: true);

        Assert.True(correlations.Count <= OperationalTriageAggregation.MaxCorrelations);
        Assert.Contains(correlations, c => c.CorrelationLabel == OperationalTriageAggregation.CorrelationReplayTrend);
    }

    [Fact]
    public void Triage_summary_uses_operator_wording_only()
    {
        var recommendations = OperationalTriageAggregation.ComposeRecommendations(
            new OperationalNavigationIndexDto(),
            new OperationalTimelineDto(),
            new OperationalTrendSummaryDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        Assert.All(recommendations, r =>
        {
            Assert.DoesNotContain("Pipeline", r.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Governance", r.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Explainability", r.RecommendedAction, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Triage_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalTriageService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTriageService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Triage_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTriageService.cs"));
        Assert.Contains("Operational triage observability:", service, StringComparison.Ordinal);
    }
}
