using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalNavigationArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Navigation_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditNavigationController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/navigation", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"routes\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditNavigationController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalNavigation");
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
    public void Navigation_service_uses_composition_hub_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalNavigationService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_aggregation_limits_recommendations_and_orders_deterministically()
    {
        var dashboard = new OperationalDashboardSummaryDto
        {
            Health = new OperationalDashboardHealthDto { State = OperationalDashboardHealthState.Critical }
        };
        var reconciliation = new OperationalReconciliationWorkbenchDto
        {
            Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 3 },
            ReplayRisk = new OperationalReconciliationReplayRiskDto { ReplayEscalationObserved = true }
        };
        var inventory = new OperationalInventoryWorkbenchDto
        {
            DriftSummary = new OperationalInventoryDriftSummaryDto
            {
                DriftSeverity = OperationalInventoryDriftSeverity.Critical,
                EscalatingDriftConflicts = 4
            }
        };
        var replayPressure = new OperationalReplayPressureSummaryDto
        {
            InstabilityLevel = OperationalReplayPressureLevel.Critical,
            ProtectiveModeVisible = true
        };
        var replayStabilization = new OperationalReplayStabilizationDto { ReplayPressureEscalating = true };
        var trend = new OperationalTrendSummaryDto
        {
            OverallDirection = OperationalTrendDirection.Degrading,
            Severity = OperationalTrendSeverity.High
        };
        var readiness = new OperationalNavigationReadinessSignals { RuntimeProtectionActive = true };

        var recommendations = OperationalNavigationAggregation.ComposeRecommendations(
            dashboard,
            reconciliation,
            inventory,
            replayPressure,
            replayStabilization,
            trend,
            readiness);
        var attention = OperationalNavigationAggregation.ComposeAttentionItems(
            dashboard,
            reconciliation,
            inventory,
            replayPressure,
            replayStabilization,
            trend,
            readiness);

        Assert.True(recommendations.Count <= OperationalNavigationAggregation.MaxRecommendations);
        Assert.True(attention.Count <= OperationalNavigationAggregation.MaxAttentionItems);
        Assert.Equal(
            recommendations.OrderBy(r => r.Priority).ThenBy(r => r.Title, StringComparer.Ordinal).ToList(),
            recommendations.ToList());
        Assert.Equal(
            attention.OrderBy(a => a.Priority).ThenBy(a => a.Title, StringComparer.Ordinal).ToList(),
            attention.ToList());
        Assert.Equal(OperationalNavigationAggregation.RouteReplayWorkbench, recommendations[0].RelativeRoute);
    }

    [Fact]
    public void Navigation_routes_use_existing_operational_paths_only()
    {
        var routes = OperationalNavigationAggregation.ComposeRoutes(
            new OperationalDashboardSummaryDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalTrendSummaryDto(),
            new OperationalNavigationReadinessSignals());

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalNavigationAggregation.RouteDashboard,
            OperationalNavigationAggregation.RouteReconciliationWorkbench,
            OperationalNavigationAggregation.RouteInventoryWorkbench,
            OperationalNavigationAggregation.RouteReplayWorkbench,
            OperationalNavigationAggregation.RouteTrendSummary
        };

        Assert.All(routes, route => Assert.Contains(route.RelativeRoute, allowed));
    }

    [Fact]
    public void Navigation_sections_include_required_domains()
    {
        var sections = OperationalNavigationAggregation.ComposeSections(
            new OperationalDashboardSummaryDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalTrendSummaryDto(),
            new OperationalNavigationReadinessSignals(),
            new OperationalGovernanceRuntimeProtectionSnapshot());

        var names = sections.Select(s => s.SectionName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(OperationalNavigationAggregation.SectionSystemHealth, names);
        Assert.Contains(OperationalNavigationAggregation.SectionReplayStability, names);
        Assert.Contains(OperationalNavigationAggregation.SectionInventoryDrift, names);
        Assert.Contains(OperationalNavigationAggregation.SectionReconciliationPressure, names);
        Assert.Contains(OperationalNavigationAggregation.SectionRuntimeProtection, names);
        Assert.Contains(OperationalNavigationAggregation.SectionTrendStability, names);
    }

    [Fact]
    public void Navigation_summary_uses_operator_wording_only()
    {
        var index = OperationalNavigationAggregation.ComposeIndex(
            new OperationalDashboardSummaryDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalTrendSummaryDto(),
            new OperationalNavigationReadinessSignals(),
            new OperationalGovernanceRuntimeProtectionSnapshot());

        Assert.DoesNotContain("Pipeline", index.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", index.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", index.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cache", index.NavigationNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Navigation_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalNavigationService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalNavigationService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalNavigationService.cs"));
        Assert.Contains("Operational navigation observability:", service, StringComparison.Ordinal);
    }
}
