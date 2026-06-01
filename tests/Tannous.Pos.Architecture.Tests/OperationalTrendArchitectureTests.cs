using Tannous.Pos.Application.OperationalTrends;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalTrendArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Trend_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTrendController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/trends", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"deltas\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Trend_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTrendController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Trend_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalTrends");
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
    public void Trend_service_uses_composition_hub_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTrendService.cs");
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
    public void Trend_window_store_retains_max_three_snapshots_fifo()
    {
        var store = new Tannous.Pos.Infrastructure.Services.OperationalTrends.OperationalTrendWindowStore();

        for (var i = 0; i < 5; i++)
        {
            store.Append(new OperationalTrendSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow.AddMinutes(i),
                FingerprintId = $"fp-{i}",
                UnresolvedReconciliationCount = i
            });
        }

        var snapshots = store.GetSnapshots();
        Assert.Equal(OperationalTrendAggregation.MaxWindowSnapshots, snapshots.Count);
        Assert.Equal("fp-2", snapshots[0].FingerprintId);
        Assert.Equal("fp-4", snapshots[^1].FingerprintId);
    }

    [Fact]
    public void Trend_snapshot_does_not_retain_payload_fields()
    {
        var snapshotType = typeof(OperationalTrendSnapshot);
        var forbidden = new[]
        {
            "Payload",
            "EntityId",
            "Receipt",
            "Export",
            "CacheKey",
            "Timeline",
            "OperationId",
            "OrderId"
        };

        foreach (var property in snapshotType.GetProperties())
        {
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, property.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Trend_aggregation_limits_attention_items_and_orders_deterministically()
    {
        var current = new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            ActiveReplayPressure = 6,
            ProtectiveModeActive = true,
            InventoryDriftConflictCount = 4,
            FingerprintStability = "Volatile",
            FingerprintId = "current"
        };
        var prior = new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ActiveReplayPressure = 2,
            ProtectiveModeActive = false,
            InventoryDriftConflictCount = 1,
            FingerprintStability = "Stable",
            FingerprintId = "prior"
        };

        var delta = OperationalTrendAggregation.CompareSnapshots(current, prior);
        var attention = OperationalTrendAggregation.ComposeAttentionItems(current, prior, delta);

        Assert.True(attention.Count <= OperationalTrendAggregation.MaxAttentionItems);
        Assert.Equal(OperationalTrendDirection.Degrading, delta.OverallDirection);
        Assert.Equal(
            attention.OrderBy(i => i.Priority).ThenBy(i => i.Title, StringComparer.Ordinal).ToList(),
            attention.ToList());
    }

    [Fact]
    public void Trend_comparison_is_stable_for_unchanged_snapshots()
    {
        var snapshot = new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            FingerprintId = "stable-fp",
            FingerprintStability = "Stable",
            ReadinessState = "Ready",
            PressureBand = "Nominal",
            UnresolvedReconciliationCount = 1,
            InventoryDriftConflictCount = 0,
            ActiveReplayPressure = 0,
            ReplayInstabilityLevel = nameof(Tannous.Pos.Application.OperationalReplayWorkbench.OperationalReplayPressureLevel.Nominal),
            ActiveAlertCount = 0
        };

        var prior = new OperationalTrendSnapshot
        {
            CapturedAtUtc = snapshot.CapturedAtUtc.AddMinutes(-1),
            FingerprintId = snapshot.FingerprintId,
            FingerprintStability = snapshot.FingerprintStability,
            ReadinessState = snapshot.ReadinessState,
            PressureBand = snapshot.PressureBand,
            UnresolvedReconciliationCount = snapshot.UnresolvedReconciliationCount,
            InventoryDriftConflictCount = snapshot.InventoryDriftConflictCount,
            ActiveReplayPressure = snapshot.ActiveReplayPressure,
            ReplayInstabilityLevel = snapshot.ReplayInstabilityLevel,
            ActiveAlertCount = snapshot.ActiveAlertCount
        };

        var delta = OperationalTrendAggregation.CompareSnapshots(snapshot, prior);

        Assert.Equal(OperationalTrendDirection.Stable, delta.OverallDirection);
        Assert.Contains(OperationalTrendAggregation.SignalOperationalStable, delta.MovementSignals);
    }

    [Fact]
    public void Trend_improving_transition_detected_when_conflicts_decrease()
    {
        var current = new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            UnresolvedReconciliationCount = 1,
            ActiveReplayPressure = 1,
            PressureBand = "Moderate",
            ProtectiveModeActive = false
        };
        var prior = new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            UnresolvedReconciliationCount = 4,
            ActiveReplayPressure = 3,
            PressureBand = "High",
            ProtectiveModeActive = true
        };

        var delta = OperationalTrendAggregation.CompareSnapshots(current, prior);

        Assert.Equal(OperationalTrendDirection.Improving, delta.OverallDirection);
    }

    [Fact]
    public void Trend_summary_uses_operator_wording_only()
    {
        var summary = OperationalTrendAggregation.ComposeSummary(
            new OperationalTrendSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                ActiveReplayPressure = 1
            },
            Array.Empty<OperationalTrendSnapshot>());

        Assert.DoesNotContain("Pipeline", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", summary.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Trend_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalTrendService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTrendService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendWindowStore", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTrendWindowStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Trend_window_store_has_no_background_workers()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTrends",
            "OperationalTrendWindowStore.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", text, StringComparison.Ordinal);
    }
}
