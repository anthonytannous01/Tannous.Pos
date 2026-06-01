using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalDashboardAggregationTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Dashboard_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditDashboardController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/dashboard", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_service_has_no_persistence_writes()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDashboardService.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_dbContext.Add", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalDashboardService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalDashboardService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_read_models_exist_and_use_operator_facing_types()
    {
        var auditDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalDashboard");
        foreach (var file in new[]
                 {
                     "OperationalDashboardSummaryDto.cs",
                     "OperationalDashboardHealthDto.cs",
                     "OperationalDashboardRiskDto.cs",
                     "OperationalDashboardPressureDto.cs",
                     "OperationalDashboardActivityDto.cs"
                 })
        {
            Assert.True(File.Exists(Path.Combine(auditDir, file)), $"Missing {file}");
        }

        var summary = File.ReadAllText(Path.Combine(auditDir, "OperationalDashboardSummaryDto.cs"));
        Assert.Contains("OperationalDashboardHealthDto", summary, StringComparison.Ordinal);
        Assert.Contains("OperationalDashboardRiskDto", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("PipelineStage", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("ExplainabilityComposer", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_aggregation_limits_recommendations_and_orders_deterministically()
    {
        var resilience = new OperationalResilienceSummaryDto
        {
            UnresolvedConflictCount = 2,
            ReplayStormRiskIndicated = true,
            ExportTruncationPressureIndicated = true
        };
        var reconciliation = new ReconciliationSummaryDto
        {
            UnresolvedCount = 2,
            ReplayMismatchCount = 1,
            InventoryDriftRiskCount = 1
        };
        var alerts = new OperationalAlertSummaryDto { CriticalSignals = 1, WarningSignals = 2, TotalSignals = 3 };
        var overview = new OperationalCacheGovernanceOverviewDto
        {
            AgingEntryCount = 1,
            NearExpiryEntryCount = 0,
            ExpiredEntryCount = 0
        };
        var runtime = new OperationalGovernanceRuntimeProtectionDto
        {
            Failsafe = new OperationalGovernanceFailsafeDto { FailsafeActive = true }
        };

        var recommendations = OperationalDashboardAggregation.ComposeRecommendations(
            resilience,
            reconciliation,
            alerts,
            overview,
            runtime);

        Assert.True(recommendations.Count <= OperationalDashboardAggregation.MaxRecommendations);
        Assert.Equal(
            recommendations.OrderBy(r => r, StringComparer.Ordinal).ToList(),
            recommendations.OrderBy(r => r, StringComparer.Ordinal).ToList());
        Assert.Contains(recommendations, r => r.Contains("reconciliation backlog", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            "Review reconciliation backlog and triage unresolved conflicts.",
            recommendations[0]);
    }

    [Fact]
    public void Dashboard_aggregation_composes_health_without_governance_jargon_in_summary()
    {
        var health = OperationalDashboardAggregation.ComposeHealth(
            new OperationalResilienceSummaryDto(),
            new OperationalAlertSummaryDto(),
            new OperationalIncidentSummaryDto(),
            new OperationalGovernanceRuntimeProtectionDto(),
            new OperationalGovernanceFingerprintDto());

        Assert.False(string.IsNullOrWhiteSpace(health.Summary));
        Assert.DoesNotContain("Pipeline", health.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", health.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dashboard_does_not_add_cache_diagnostics_get_endpoints()
    {
        var snapshot = OperationalGovernanceSurfaceMeasurementHelper.MeasureFromRepository(RepoRoot());
        Assert.True(snapshot.CacheDiagnosticsGetEndpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }

    [Fact]
    public void Dashboard_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDashboardService.cs"));
        Assert.Contains("Operational dashboard observability:", service, StringComparison.Ordinal);
    }
}
