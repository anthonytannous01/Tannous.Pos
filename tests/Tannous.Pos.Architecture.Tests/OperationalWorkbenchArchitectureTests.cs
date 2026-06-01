using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalWorkbenchArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Workbench_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/workbench", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"reconciliation\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Workbench_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Repository", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Workbench_layer_does_not_reference_governance_internals_directly()
    {
        var workbenchDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalWorkbench");
        var files = Directory.GetFiles(workbenchDir, "*.cs", SearchOption.AllDirectories);

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
    public void Workbench_service_has_no_persistence_writes_or_parallel_orchestration()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalReconciliationWorkbenchService.cs");
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
    public void Workbench_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalReconciliationWorkbenchService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalReconciliationWorkbenchService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Workbench_read_models_exist_outside_audit_governance()
    {
        var workbenchDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalWorkbench");
        foreach (var file in new[]
                 {
                     "OperationalReconciliationWorkbenchDto.cs",
                     "OperationalReconciliationQueueDto.cs",
                     "OperationalReconciliationHotspotDto.cs",
                     "OperationalReconciliationReplayRiskDto.cs",
                     "OperationalReconciliationInventoryDriftDto.cs",
                     "OperationalReconciliationAttentionItemDto.cs"
                 })
        {
            Assert.True(File.Exists(Path.Combine(workbenchDir, file)), $"Missing {file}");
        }

        var summary = File.ReadAllText(Path.Combine(workbenchDir, "OperationalReconciliationWorkbenchDto.cs"));
        Assert.DoesNotContain("PipelineStage", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("GovernanceSnapshot", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Workbench_aggregation_limits_hotspots_and_attention_items_deterministically()
    {
        var reconciliation = new ReconciliationSummaryDto
        {
            UnresolvedCount = 3,
            InvestigatingCount = 1,
            ReplayMismatchCount = 2,
            InventoryDriftRiskCount = 1
        };
        var alerts = new OperationalAlertSummaryDto
        {
            CriticalSignals = 1,
            WarningSignals = 1,
            TotalSignals = 2,
            ReplayRelatedSignals = 1,
            InventoryRelatedSignals = 1
        };
        var resilience = new OperationalResilienceSummaryDto
        {
            ReplayStormRiskIndicated = true,
            ExportTruncationPressureIndicated = true
        };
        var incidents = new OperationalIncidentSummaryDto { ReplayIncidentCount = 1, CascadingDegradationCount = 1 };
        var overview = new OperationalCacheGovernanceOverviewDto
        {
            AgingEntryCount = 1,
            NearExpiryEntryCount = 1,
            TotalInvalidations = 5
        };
        var dashboard = new OperationalDashboardSummaryDto
        {
            Pressure = new OperationalDashboardPressureDto
            {
                ExportPressureIndicated = true,
                ProtectiveModeActive = true
            }
        };

        var hotspots = OperationalReconciliationWorkbenchAggregation.ComposeHotspots(
            reconciliation,
            alerts,
            resilience,
            overview,
            dashboard);
        var attentionItems = OperationalReconciliationWorkbenchAggregation.ComposeAttentionItems(
            reconciliation,
            alerts,
            incidents,
            resilience,
            overview,
            dashboard);

        Assert.True(hotspots.Count <= OperationalReconciliationWorkbenchAggregation.MaxHotspots);
        Assert.True(attentionItems.Count <= OperationalReconciliationWorkbenchAggregation.MaxAttentionItems);
        Assert.Equal(
            hotspots.OrderByDescending(h => h.Severity).ThenByDescending(h => h.PressureCount).ThenBy(h => h.Category, StringComparer.Ordinal).ToList(),
            hotspots.ToList());
        Assert.Equal(
            attentionItems.OrderBy(i => i.Priority).ThenBy(i => i.Title, StringComparer.Ordinal).ToList(),
            attentionItems.ToList());
    }

    [Fact]
    public void Workbench_replay_risk_projection_uses_operator_wording_only()
    {
        var replayRisk = OperationalReconciliationWorkbenchAggregation.ComposeReplayRisk(
            new OperationalResilienceSummaryDto { ReplayStormRiskIndicated = true },
            new ReconciliationSummaryDto { ReplayMismatchCount = 1 },
            new OperationalIncidentSummaryDto(),
            new OperationalDashboardSummaryDto
            {
                Pressure = new OperationalDashboardPressureDto { ProtectiveModeActive = true }
            });

        Assert.Contains("instability", replayRisk.InstabilityLevel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pipeline", replayRisk.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", replayRisk.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", replayRisk.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Workbench_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalReconciliationWorkbenchService.cs"));
        Assert.Contains("Operational workbench observability:", service, StringComparison.Ordinal);
    }
}
