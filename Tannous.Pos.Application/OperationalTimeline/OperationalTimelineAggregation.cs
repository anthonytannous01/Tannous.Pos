using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Deterministic bounded operational timeline composition from existing read models.</summary>
public static class OperationalTimelineAggregation
{
    public const int MaxTimelineEvents = 25;
    public const int MaxCorrelations = 8;
    public const int MaxAttentionItems = 8;

    public const string RouteDashboard = "dashboard";
    public const string RouteReconciliationWorkbench = "workbench/reconciliation";
    public const string RouteInventoryWorkbench = "inventory-workbench/drift";
    public const string RouteReplayWorkbench = "replay-workbench/pressure";
    public const string RouteTrendSummary = "trends/summary";

    public const string CorrelationReplayThenProtection = "ReplayDegradationProtectiveMode";
    public const string CorrelationReconciliationThenDrift = "ReconciliationPressureInventoryDrift";
    public const string CorrelationRecoveryAfterPressure = "StabilizationAfterPressure";
    public const string CorrelationTrendAfterReplay = "TrendDegradationReplayInstability";

    public static OperationalTimelineCaptureSnapshot BuildCaptureSnapshot(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection)
    {
        return new OperationalTimelineCaptureSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            ActiveReplayPressure = replayPressure.ActiveReplayPressure,
            ReplayInstabilityLevel = replayPressure.InstabilityLevel.ToString(),
            ProtectiveModeActive = dashboard.Pressure.ProtectiveModeActive
                || replayPressure.ProtectiveModeVisible
                || runtimeProtection.FailsafeActive
                || inventoryWorkbench.DriftSummary.ProtectiveModeActive,
            InventoryDriftConflictCount = inventoryWorkbench.DriftSummary.TotalInventoryDriftConflicts,
            UnresolvedReconciliationCount = dashboard.Activity.UnresolvedReconciliationCount,
            EscalatingConflictCount = reconciliationWorkbench.Queue.EscalatingConflicts,
            ReplayStabilizationActive = replayStabilization.StabilizationActive,
            ReplayRecoveryImproving = replayStabilization.ReplayRecoveryImproving,
            TrendDirection = trendSummary.OverallDirection.ToString(),
            FingerprintStability = string.IsNullOrWhiteSpace(fingerprint.FingerprintStability)
                ? "Unknown"
                : fingerprint.FingerprintStability.Trim(),
            FingerprintChanged = fingerprint.FingerprintChanged,
            HealthState = dashboard.Health.State.ToString()
        };
    }

    public static IReadOnlyList<OperationalTimelineEventRecord> DetectTransitionEvents(
        OperationalTimelineCaptureSnapshot current,
        OperationalTimelineCaptureSnapshot? prior)
    {
        if (prior is null)
        {
            return new[]
            {
                CreateEvent(
                    OperationalTimelineCategory.SystemHealth,
                    OperationalTimelineSeverity.Nominal,
                    OperationalTimelineDirection.Stable,
                    "Operational monitoring baseline captured",
                    "BaselineCapture",
                    RouteDashboard)
            };
        }

        var events = new List<OperationalTimelineEventRecord>();

        if (current.ActiveReplayPressure > prior.ActiveReplayPressure
            || RankReplayInstability(current.ReplayInstabilityLevel) > RankReplayInstability(prior.ReplayInstabilityLevel))
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.ReplayPressure,
                MapReplaySeverity(current.ReplayInstabilityLevel),
                OperationalTimelineDirection.Degrading,
                "Replay pressure increased",
                CorrelationTrendAfterReplay,
                RouteReplayWorkbench));
        }
        else if (current.ActiveReplayPressure < prior.ActiveReplayPressure
                 && RankReplayInstability(current.ReplayInstabilityLevel) <= RankReplayInstability(prior.ReplayInstabilityLevel))
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.ReplayPressure,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Recovered,
                "Runtime pressure reduced",
                CorrelationRecoveryAfterPressure,
                RouteReplayWorkbench));
        }

        if (current.ProtectiveModeActive && !prior.ProtectiveModeActive)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.RuntimeProtection,
                OperationalTimelineSeverity.High,
                OperationalTimelineDirection.Activated,
                "Protective mode activated",
                CorrelationReplayThenProtection,
                RouteDashboard));
        }
        else if (!current.ProtectiveModeActive && prior.ProtectiveModeActive)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.RuntimeProtection,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Recovered,
                "Protective mode cleared",
                CorrelationRecoveryAfterPressure,
                RouteDashboard));
        }

        if (current.InventoryDriftConflictCount > prior.InventoryDriftConflictCount)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.InventoryDrift,
                ClassifyDriftSeverity(current.InventoryDriftConflictCount),
                OperationalTimelineDirection.Degrading,
                "Inventory drift escalation observed",
                prior.EscalatingConflictCount > 0 || prior.UnresolvedReconciliationCount > 0
                    ? CorrelationReconciliationThenDrift
                    : "InventoryDriftEscalation",
                RouteInventoryWorkbench));
        }
        else if (current.InventoryDriftConflictCount < prior.InventoryDriftConflictCount)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.InventoryDrift,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Improving,
                "Inventory drift stabilized",
                "InventoryDriftRecovery",
                RouteInventoryWorkbench));
        }

        if (current.EscalatingConflictCount > prior.EscalatingConflictCount
            || current.UnresolvedReconciliationCount > prior.UnresolvedReconciliationCount)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.ReconciliationPressure,
                ClassifyReconciliationSeverity(current),
                OperationalTimelineDirection.Degrading,
                "Reconciliation pressure escalating",
                "ReconciliationEscalation",
                RouteReconciliationWorkbench));
        }
        else if (current.UnresolvedReconciliationCount < prior.UnresolvedReconciliationCount)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.ReconciliationPressure,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Improving,
                "Reconciliation backlog reduced",
                "ReconciliationRecovery",
                RouteReconciliationWorkbench));
        }

        if (current.ReplayRecoveryImproving && !prior.ReplayRecoveryImproving)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.Stabilization,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Improving,
                "Stabilization improvement observed",
                CorrelationRecoveryAfterPressure,
                RouteReplayWorkbench));
        }
        else if (current.ReplayStabilizationActive && !prior.ReplayStabilizationActive)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.Stabilization,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Improving,
                "Operational state stabilized",
                "StabilizationActive",
                RouteReplayWorkbench));
        }

        if (!string.Equals(current.TrendDirection, prior.TrendDirection, StringComparison.Ordinal)
            && string.Equals(current.TrendDirection, nameof(OperationalTrendDirection.Degrading), StringComparison.Ordinal))
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.TrendMovement,
                OperationalTimelineSeverity.Elevated,
                OperationalTimelineDirection.Degrading,
                "Short-window trend degradation observed",
                RankReplayInstability(current.ReplayInstabilityLevel) > RankReplayInstability(prior.ReplayInstabilityLevel)
                    ? CorrelationTrendAfterReplay
                    : "TrendDegradation",
                RouteTrendSummary));
        }
        else if (!string.Equals(current.TrendDirection, prior.TrendDirection, StringComparison.Ordinal)
                 && string.Equals(current.TrendDirection, nameof(OperationalTrendDirection.Improving), StringComparison.Ordinal))
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.TrendMovement,
                OperationalTimelineSeverity.Moderate,
                OperationalTimelineDirection.Improving,
                "Short-window trend improving",
                "TrendImprovement",
                RouteTrendSummary));
        }

        if (current.FingerprintChanged && !prior.FingerprintChanged)
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.SystemHealth,
                OperationalTimelineSeverity.Elevated,
                OperationalTimelineDirection.Degrading,
                "Operational fingerprint transition observed",
                "FingerprintTransition",
                RouteDashboard));
        }

        if (events.Count == 0 && IsStableCapture(current, prior))
        {
            events.Add(CreateEvent(
                OperationalTimelineCategory.SystemHealth,
                OperationalTimelineSeverity.Nominal,
                OperationalTimelineDirection.Stable,
                "Operational state stable",
                "StableMonitoring",
                RouteDashboard));
        }

        return events
            .OrderBy(e => RankEventPriority(e))
            .ThenBy(e => e.Category)
            .ThenBy(e => e.Summary, StringComparer.Ordinal)
            .ToList();
    }

    public static OperationalTimelineDto ComposeTimeline(
        IReadOnlyList<OperationalTimelineEventRecord> events)
    {
        var ordered = events
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => RankEventPriority(e))
            .ThenBy(e => e.Summary, StringComparer.Ordinal)
            .Take(MaxTimelineEvents)
            .Select(MapEvent)
            .ToList();

        var attention = ComposeAttentionItems(events);

        return new OperationalTimelineDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            EventCount = ordered.Count,
            MaxEvents = MaxTimelineEvents,
            Events = ordered,
            AttentionItems = attention,
            Summary = DescribeTimelineSummary(ordered)
        };
    }

    public static IReadOnlyList<OperationalTimelineCorrelationDto> ComposeCorrelations(
        IReadOnlyList<OperationalTimelineEventRecord> events)
    {
        if (events.Count == 0)
            return Array.Empty<OperationalTimelineCorrelationDto>();

        var ordered = events.OrderBy(e => e.OccurredAtUtc).ToList();
        var correlations = new List<(int Priority, OperationalTimelineCorrelationDto Item)>();

        if (ContainsSequence(ordered, OperationalTimelineCategory.ReplayPressure, OperationalTimelineDirection.Degrading,
                OperationalTimelineCategory.RuntimeProtection, OperationalTimelineDirection.Activated))
        {
            correlations.Add((1, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = CorrelationReplayThenProtection,
                Summary = "Replay degradation followed by protective mode activation",
                Severity = OperationalTimelineSeverity.High,
                RelatedCategories = new[] { nameof(OperationalTimelineCategory.ReplayPressure), nameof(OperationalTimelineCategory.RuntimeProtection) },
                SuggestedRoute = RouteReplayWorkbench
            }));
        }

        if (ContainsSequence(ordered, OperationalTimelineCategory.ReconciliationPressure, OperationalTimelineDirection.Degrading,
                OperationalTimelineCategory.InventoryDrift, OperationalTimelineDirection.Degrading))
        {
            correlations.Add((2, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = CorrelationReconciliationThenDrift,
                Summary = "Inventory drift escalation after reconciliation pressure",
                Severity = OperationalTimelineSeverity.Elevated,
                RelatedCategories = new[] { nameof(OperationalTimelineCategory.ReconciliationPressure), nameof(OperationalTimelineCategory.InventoryDrift) },
                SuggestedRoute = RouteInventoryWorkbench
            }));
        }

        if (ContainsRecoveryAfterPressure(ordered))
        {
            correlations.Add((3, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = CorrelationRecoveryAfterPressure,
                Summary = "Stabilization improvement after pressure recovery",
                Severity = OperationalTimelineSeverity.Moderate,
                RelatedCategories = new[] { nameof(OperationalTimelineCategory.Stabilization), nameof(OperationalTimelineCategory.ReplayPressure) },
                SuggestedRoute = RouteReplayWorkbench
            }));
        }

        if (ContainsSequence(ordered, OperationalTimelineCategory.ReplayPressure, OperationalTimelineDirection.Degrading,
                OperationalTimelineCategory.TrendMovement, OperationalTimelineDirection.Degrading))
        {
            correlations.Add((4, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = CorrelationTrendAfterReplay,
                Summary = "Trend degradation after replay instability",
                Severity = OperationalTimelineSeverity.Elevated,
                RelatedCategories = new[] { nameof(OperationalTimelineCategory.ReplayPressure), nameof(OperationalTimelineCategory.TrendMovement) },
                SuggestedRoute = RouteTrendSummary
            }));
        }

        foreach (var group in ordered
                     .Where(e => !string.IsNullOrWhiteSpace(e.CorrelationLabel))
                     .GroupBy(e => e.CorrelationLabel, StringComparer.Ordinal)
                     .Where(g => g.Count() >= 2))
        {
            correlations.Add((10, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = group.Key,
                Summary = $"Repeated {FormatCategory(group.First().Category)} movement within short window",
                Severity = group.Max(e => e.Severity),
                RelatedCategories = group.Select(e => e.Category.ToString()).Distinct(StringComparer.Ordinal).ToList(),
                SuggestedRoute = group.OrderByDescending(e => e.Severity).First().SuggestedRoute
            }));
        }

        if (correlations.Count == 0 && ordered.Count > 0)
        {
            correlations.Add((50, new OperationalTimelineCorrelationDto
            {
                CorrelationLabel = "StableMonitoring",
                Summary = "No correlated degradation sequence detected in recent timeline window",
                Severity = OperationalTimelineSeverity.Nominal,
                RelatedCategories = new[] { nameof(OperationalTimelineCategory.SystemHealth) },
                SuggestedRoute = RouteDashboard
            }));
        }

        return correlations
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Item.CorrelationLabel, StringComparer.Ordinal)
            .Take(MaxCorrelations)
            .Select(c => c.Item)
            .ToList();
    }

    public static IReadOnlyList<OperationalTimelineAttentionDto> ComposeAttentionItems(
        IReadOnlyList<OperationalTimelineEventRecord> events)
    {
        var items = events
            .OrderByDescending(e => e.Severity)
            .ThenByDescending(e => e.OccurredAtUtc)
            .Select((e, index) => new OperationalTimelineAttentionDto
            {
                Priority = RankEventPriority(e) + index,
                Severity = e.Severity,
                Category = e.Category,
                Title = FormatAttentionTitle(e),
                Detail = e.Summary,
                SuggestedRoute = e.SuggestedRoute
            })
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .ToList();

        if (items.Count == 0)
        {
            items.Add(new OperationalTimelineAttentionDto
            {
                Priority = 100,
                Severity = OperationalTimelineSeverity.Nominal,
                Category = OperationalTimelineCategory.SystemHealth,
                Title = "Timeline monitoring active",
                Detail = "No recent operational transitions recorded in the bounded timeline window.",
                SuggestedRoute = RouteDashboard
            });
        }

        return items;
    }

    private static OperationalTimelineEventRecord CreateEvent(
        OperationalTimelineCategory category,
        OperationalTimelineSeverity severity,
        OperationalTimelineDirection direction,
        string summary,
        string correlationLabel,
        string suggestedRoute) =>
        new()
        {
            OccurredAtUtc = DateTime.UtcNow,
            Category = category,
            Severity = severity,
            Direction = direction,
            Summary = summary,
            CorrelationLabel = correlationLabel,
            SuggestedRoute = suggestedRoute
        };

    private static OperationalTimelineEventDto MapEvent(OperationalTimelineEventRecord record) =>
        new()
        {
            OccurredAtUtc = record.OccurredAtUtc,
            Category = record.Category,
            Severity = record.Severity,
            Direction = record.Direction,
            Summary = record.Summary,
            CorrelationLabel = record.CorrelationLabel,
            SuggestedRoute = record.SuggestedRoute
        };

    private static int RankEventPriority(OperationalTimelineEventRecord record) => record.Category switch
    {
        OperationalTimelineCategory.ReplayPressure when record.Severity >= OperationalTimelineSeverity.High => 1,
        OperationalTimelineCategory.RuntimeProtection when record.Direction == OperationalTimelineDirection.Activated => 2,
        OperationalTimelineCategory.InventoryDrift when record.Severity >= OperationalTimelineSeverity.High => 3,
        OperationalTimelineCategory.ReconciliationPressure when record.Direction == OperationalTimelineDirection.Degrading => 4,
        OperationalTimelineCategory.Stabilization when record.Direction == OperationalTimelineDirection.Improving => 5,
        _ => 6
    };

    private static bool ContainsSequence(
        IReadOnlyList<OperationalTimelineEventRecord> ordered,
        OperationalTimelineCategory firstCategory,
        OperationalTimelineDirection firstDirection,
        OperationalTimelineCategory secondCategory,
        OperationalTimelineDirection secondDirection)
    {
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var first = ordered[i];
            if (first.Category != firstCategory || first.Direction != firstDirection)
                continue;

            for (var j = i + 1; j < ordered.Count; j++)
            {
                var second = ordered[j];
                if (second.Category == secondCategory && second.Direction == secondDirection)
                    return true;
            }
        }

        return false;
    }

    private static bool ContainsRecoveryAfterPressure(IReadOnlyList<OperationalTimelineEventRecord> ordered)
    {
        var pressureIndex = -1;
        for (var i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            if (item.Category == OperationalTimelineCategory.ReplayPressure
                && item.Direction == OperationalTimelineDirection.Degrading)
            {
                pressureIndex = i;
                break;
            }
        }

        if (pressureIndex < 0)
            return false;

        return ordered.Skip(pressureIndex + 1).Any(e =>
            (e.Category == OperationalTimelineCategory.Stabilization && e.Direction == OperationalTimelineDirection.Improving)
            || (e.Category == OperationalTimelineCategory.ReplayPressure && e.Direction == OperationalTimelineDirection.Recovered));
    }

    private static bool IsStableCapture(
        OperationalTimelineCaptureSnapshot current,
        OperationalTimelineCaptureSnapshot prior) =>
        current.ActiveReplayPressure == prior.ActiveReplayPressure
        && current.ProtectiveModeActive == prior.ProtectiveModeActive
        && current.InventoryDriftConflictCount == prior.InventoryDriftConflictCount
        && current.UnresolvedReconciliationCount == prior.UnresolvedReconciliationCount
        && current.EscalatingConflictCount == prior.EscalatingConflictCount
        && string.Equals(current.TrendDirection, prior.TrendDirection, StringComparison.Ordinal);

    private static OperationalTimelineSeverity MapReplaySeverity(string instabilityLevel) => instabilityLevel switch
    {
        nameof(OperationalReplayPressureLevel.Critical) => OperationalTimelineSeverity.Critical,
        nameof(OperationalReplayPressureLevel.High) => OperationalTimelineSeverity.High,
        nameof(OperationalReplayPressureLevel.Elevated) => OperationalTimelineSeverity.Elevated,
        _ => OperationalTimelineSeverity.Moderate
    };

    private static OperationalTimelineSeverity ClassifyDriftSeverity(int driftCount) =>
        driftCount >= 4 ? OperationalTimelineSeverity.Critical
        : driftCount >= 2 ? OperationalTimelineSeverity.High
        : OperationalTimelineSeverity.Elevated;

    private static OperationalTimelineSeverity ClassifyReconciliationSeverity(OperationalTimelineCaptureSnapshot capture) =>
        capture.EscalatingConflictCount >= 3 ? OperationalTimelineSeverity.Critical
        : capture.EscalatingConflictCount > 0 ? OperationalTimelineSeverity.High
        : OperationalTimelineSeverity.Elevated;

    private static int RankReplayInstability(string level) => level switch
    {
        nameof(OperationalReplayPressureLevel.Critical) => 3,
        nameof(OperationalReplayPressureLevel.High) => 2,
        nameof(OperationalReplayPressureLevel.Elevated) => 1,
        _ => 0
    };

    private static string DescribeTimelineSummary(IReadOnlyList<OperationalTimelineEventDto> events)
    {
        if (events.Count == 0)
            return "No operational timeline events recorded yet.";

        var latest = events[^1];
        return latest.Direction switch
        {
            OperationalTimelineDirection.Degrading => $"Latest transition: {latest.Summary}. Review suggested route for detail.",
            OperationalTimelineDirection.Improving or OperationalTimelineDirection.Recovered =>
                $"Latest transition: {latest.Summary}. Stabilization movement observed.",
            OperationalTimelineDirection.Activated =>
                $"Latest transition: {latest.Summary}. Protective conditions require operator review.",
            _ => $"Latest transition: {latest.Summary}."
        };
    }

    private static string FormatAttentionTitle(OperationalTimelineEventRecord record) =>
        $"{FormatCategory(record.Category)}: {record.Summary}";

    private static string FormatCategory(OperationalTimelineCategory category) => category switch
    {
        OperationalTimelineCategory.ReplayPressure => "Replay pressure",
        OperationalTimelineCategory.RuntimeProtection => "Runtime protection",
        OperationalTimelineCategory.InventoryDrift => "Inventory drift",
        OperationalTimelineCategory.ReconciliationPressure => "Reconciliation pressure",
        OperationalTimelineCategory.Stabilization => "Stabilization",
        OperationalTimelineCategory.TrendMovement => "Trend movement",
        _ => "System health"
    };
}
