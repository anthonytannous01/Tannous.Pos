using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalInventoryWorkbenchArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Inventory_workbench_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditInventoryWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/inventory-workbench", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"drift\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_workbench_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditInventoryWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Repository", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_workbench_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalInventoryWorkbench");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tannous.Pos.Application.Audit.Governance", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceProjectionPipeline", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceSnapshotBuilder", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExplainabilityComposer", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Inventory_workbench_service_has_no_persistence_writes_or_parallel_orchestration()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalInventoryWorkbenchService.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_dbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDiagnosticsCache>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalDiagnosticsCacheConstants", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_workbench_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalInventoryWorkbenchService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalInventoryWorkbenchService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_workbench_read_models_exist_outside_audit_governance()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalInventoryWorkbench");
        foreach (var file in new[]
                 {
                     "OperationalInventoryWorkbenchDto.cs",
                     "OperationalInventoryDriftSummaryDto.cs",
                     "OperationalInventoryDriftHotspotDto.cs",
                     "OperationalInventoryResolutionReadinessDto.cs",
                     "OperationalInventoryMismatchCategoryDto.cs",
                     "OperationalInventoryAttentionItemDto.cs"
                 })
        {
            Assert.True(File.Exists(Path.Combine(dir, file)), $"Missing {file}");
        }

        var summary = File.ReadAllText(Path.Combine(dir, "OperationalInventoryWorkbenchDto.cs"));
        Assert.DoesNotContain("PipelineStage", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("GovernanceSnapshot", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_workbench_aggregation_limits_hotspots_and_orders_deterministically()
    {
        var reconciliation = new ReconciliationSummaryDto
        {
            InventoryDriftRiskCount = 3,
            ReplayMismatchCount = 2,
            UnresolvedCount = 2,
            InvestigatingCount = 1
        };
        var alerts = new OperationalAlertSummaryDto
        {
            InventoryRelatedSignals = 2,
            ReplayRelatedSignals = 1,
            CriticalSignals = 1
        };
        var resilience = new OperationalResilienceSummaryDto { ReplayStormRiskIndicated = true };
        var incidents = new OperationalIncidentSummaryDto { CascadingDegradationCount = 1 };
        var overview = new OperationalCacheGovernanceOverviewDto
        {
            AgingEntryCount = 1,
            NearExpiryEntryCount = 1,
            TotalInvalidations = 4
        };
        var dashboard = new OperationalDashboardSummaryDto
        {
            Pressure = new OperationalDashboardPressureDto
            {
                ExportPressureIndicated = true,
                ProtectiveModeActive = true
            }
        };
        var reconciliationWorkbench = new OperationalReconciliationWorkbenchDto
        {
            Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 },
            ReplayRisk = new OperationalReconciliationReplayRiskDto { ProtectiveModeActive = true },
            InventoryDrift = new OperationalReconciliationInventoryDriftDto { ManualReviewRecommended = true }
        };

        var hotspots = OperationalInventoryWorkbenchAggregation.ComposeHotspots(
            reconciliation,
            alerts,
            resilience,
            incidents,
            overview,
            dashboard,
            reconciliationWorkbench);
        var attentionItems = OperationalInventoryWorkbenchAggregation.ComposeAttentionItems(
            reconciliation,
            alerts,
            resilience,
            incidents,
            overview,
            dashboard,
            reconciliationWorkbench);

        Assert.True(hotspots.Count <= OperationalInventoryWorkbenchAggregation.MaxHotspots);
        Assert.True(attentionItems.Count <= OperationalInventoryWorkbenchAggregation.MaxAttentionItems);
        Assert.Equal(
            hotspots
                .OrderByDescending(h => h.Severity)
                .ThenByDescending(h => h.PressureCount)
                .ThenBy(h => h.Category, StringComparer.Ordinal)
                .ToList(),
            hotspots.ToList());
        Assert.Equal(
            attentionItems.OrderBy(i => i.Priority).ThenBy(i => i.Title, StringComparer.Ordinal).ToList(),
            attentionItems.ToList());
    }

    [Fact]
    public void Inventory_workbench_drift_summary_uses_operator_severity_wording()
    {
        var summary = OperationalInventoryWorkbenchAggregation.ComposeDriftSummary(
            new ReconciliationSummaryDto { InventoryDriftRiskCount = 2 },
            new OperationalAlertSummaryDto(),
            new OperationalResilienceSummaryDto(),
            new OperationalIncidentSummaryDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReconciliationWorkbenchDto());

        Assert.NotEqual(default, summary.DriftSeverity);
        Assert.DoesNotContain("Pipeline", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", summary.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inventory_workbench_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalInventoryWorkbenchService.cs"));
        Assert.Contains("Operational inventory workbench observability:", service, StringComparison.Ordinal);
    }
}
