using Tannous.Pos.Application.OperationalTimeline;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalTimelineArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Timeline_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTimelineController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/timeline", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"correlations\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Timeline_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTimelineController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Timeline_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalTimeline");
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
    public void Timeline_service_uses_composition_hub_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTimelineService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Timeline_window_store_retains_max_twenty_five_events_fifo()
    {
        var store = new Tannous.Pos.Infrastructure.Services.OperationalTimeline.OperationalTimelineWindowStore();

        for (var i = 0; i < 30; i++)
        {
            store.Append(new OperationalTimelineEventRecord
            {
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(i),
                Summary = $"event-{i}",
                Category = OperationalTimelineCategory.SystemHealth
            });
        }

        var events = store.GetEvents();
        Assert.Equal(OperationalTimelineAggregation.MaxTimelineEvents, events.Count);
        Assert.Equal("event-5", events[0].Summary);
        Assert.Equal("event-29", events[^1].Summary);
    }

    [Fact]
    public void Timeline_event_record_does_not_retain_payload_fields()
    {
        var eventType = typeof(OperationalTimelineEventRecord);
        var forbidden = new[] { "Payload", "EntityId", "Receipt", "Export", "CacheKey", "OperationId", "OrderId" };

        foreach (var property in eventType.GetProperties())
        {
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, property.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Timeline_detects_replay_and_protective_mode_transitions()
    {
        var prior = new OperationalTimelineCaptureSnapshot
        {
            ActiveReplayPressure = 1,
            ReplayInstabilityLevel = "Elevated",
            ProtectiveModeActive = false
        };
        var current = new OperationalTimelineCaptureSnapshot
        {
            ActiveReplayPressure = 4,
            ReplayInstabilityLevel = "High",
            ProtectiveModeActive = true
        };

        var events = OperationalTimelineAggregation.DetectTransitionEvents(current, prior);

        Assert.Contains(events, e => e.Summary == "Replay pressure increased");
        Assert.Contains(events, e => e.Summary == "Protective mode activated");
    }

    [Fact]
    public void Timeline_correlations_are_bounded_and_ordered()
    {
        var events = new List<OperationalTimelineEventRecord>
        {
            new()
            {
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-2),
                Category = OperationalTimelineCategory.ReplayPressure,
                Direction = OperationalTimelineDirection.Degrading,
                Severity = OperationalTimelineSeverity.High,
                Summary = "Replay pressure increased",
                CorrelationLabel = OperationalTimelineAggregation.CorrelationTrendAfterReplay,
                SuggestedRoute = OperationalTimelineAggregation.RouteReplayWorkbench
            },
            new()
            {
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-1),
                Category = OperationalTimelineCategory.RuntimeProtection,
                Direction = OperationalTimelineDirection.Activated,
                Severity = OperationalTimelineSeverity.High,
                Summary = "Protective mode activated",
                CorrelationLabel = OperationalTimelineAggregation.CorrelationReplayThenProtection,
                SuggestedRoute = OperationalTimelineAggregation.RouteDashboard
            }
        };

        var correlations = OperationalTimelineAggregation.ComposeCorrelations(events);

        Assert.True(correlations.Count <= OperationalTimelineAggregation.MaxCorrelations);
        Assert.Contains(correlations, c => c.CorrelationLabel == OperationalTimelineAggregation.CorrelationReplayThenProtection);
    }

    [Fact]
    public void Timeline_summary_uses_operator_wording_only()
    {
        var timeline = OperationalTimelineAggregation.ComposeTimeline(new[]
        {
            new OperationalTimelineEventRecord
            {
                OccurredAtUtc = DateTime.UtcNow,
                Summary = "Operational state stable",
                Direction = OperationalTimelineDirection.Stable,
                Category = OperationalTimelineCategory.SystemHealth
            }
        });

        Assert.DoesNotContain("Pipeline", timeline.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Governance", timeline.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explainability", timeline.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Timeline_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalTimelineService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTimelineService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineWindowStore", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTimelineWindowStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Timeline_window_store_has_no_background_workers()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTimeline",
            "OperationalTimelineWindowStore.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", text, StringComparison.Ordinal);
    }
}
