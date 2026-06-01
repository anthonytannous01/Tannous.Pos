using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalReplayWorkbenchArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Replay_workbench_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditReplayWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/replay-workbench", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"pressure\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_workbench_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditReplayWorkbenchController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Repository", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_workbench_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalReplayWorkbench");
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
    public void Replay_workbench_service_has_no_persistence_writes_or_parallel_orchestration()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalReplayWorkbenchService.cs");
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
    public void Replay_workbench_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalReplayWorkbenchService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalReplayWorkbenchService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_workbench_read_models_exist_outside_audit_governance()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalReplayWorkbench");
        foreach (var file in new[]
                 {
                     "OperationalReplayWorkbenchDto.cs",
                     "OperationalReplayPressureSummaryDto.cs",
                     "OperationalReplayStabilizationDto.cs",
                     "OperationalReplayHotspotDto.cs",
                     "OperationalReplayRecoveryConfidenceDto.cs",
                     "OperationalReplayAttentionItemDto.cs"
                 })
        {
            Assert.True(File.Exists(Path.Combine(dir, file)), $"Missing {file}");
        }

        var summary = File.ReadAllText(Path.Combine(dir, "OperationalReplayWorkbenchDto.cs"));
        Assert.DoesNotContain("PipelineStage", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("GovernanceSnapshot", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_workbench_aggregation_limits_hotspots_and_orders_deterministically()
    {
        var resilience = new OperationalResilienceSummaryDto { ReplayStormRiskIndicated = true };
        var reconciliation = new ReconciliationSummaryDto { ReplayMismatchCount = 2, InventoryDriftRiskCount = 1 };
        var alerts = new OperationalAlertSummaryDto { ReplayRelatedSignals = 2, CriticalSignals = 1 };
        var incidents = new OperationalIncidentSummaryDto { ReplayIncidentCount = 1, CascadingDegradationCount = 1 };
        var overview = new OperationalCacheGovernanceOverviewDto
        {
            AgingEntryCount = 1,
            TotalInvalidations = 5
        };
        var dashboard = new OperationalDashboardSummaryDto
        {
            Pressure = new OperationalDashboardPressureDto
            {
                ProtectiveModeActive = true,
                ExportPressureIndicated = true
            }
        };
        var reconciliationWorkbench = new OperationalReconciliationWorkbenchDto
        {
            Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 },
            ReplayRisk = new OperationalReconciliationReplayRiskDto
            {
                ProtectiveModeActive = true,
                ReplayEscalationObserved = true
            }
        };
        var inventoryWorkbench = new OperationalInventoryWorkbenchDto
        {
            DriftSummary = new OperationalInventoryDriftSummaryDto { ReplayLinkedDriftPressure = 2 }
        };
        var runtimeSignals = new OperationalReplayRuntimeSignals { ProtectiveContainmentActive = true };

        var pressureSummary = OperationalReplayWorkbenchAggregation.ComposePressureSummary(
            resilience,
            reconciliation,
            alerts,
            incidents,
            dashboard,
            reconciliationWorkbench,
            runtimeSignals);
        var stabilization = OperationalReplayWorkbenchAggregation.ComposeStabilization(
            resilience,
            reconciliation,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSignals,
            pressureSummary);
        var hotspots = OperationalReplayWorkbenchAggregation.ComposeHotspots(
            resilience,
            reconciliation,
            alerts,
            incidents,
            overview,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench);
        var attentionItems = OperationalReplayWorkbenchAggregation.ComposeAttentionItems(
            resilience,
            reconciliation,
            alerts,
            incidents,
            overview,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            stabilization,
            pressureSummary);

        Assert.True(hotspots.Count <= OperationalReplayWorkbenchAggregation.MaxHotspots);
        Assert.True(attentionItems.Count <= OperationalReplayWorkbenchAggregation.MaxAttentionItems);
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
    public void Replay_workbench_pressure_summary_uses_operator_wording_only()
    {
        var summary = OperationalReplayWorkbenchAggregation.ComposePressureSummary(
            new OperationalResilienceSummaryDto(),
            new ReconciliationSummaryDto(),
            new OperationalAlertSummaryDto(),
            new OperationalIncidentSummaryDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalReplayRuntimeSignals());

        Assert.DoesNotContain("Pipeline", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", summary.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Replay_workbench_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalReplayWorkbenchService.cs"));
        Assert.Contains("Operational replay workbench observability:", service, StringComparison.Ordinal);
    }
}
