using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>Deterministic operator triage queue from existing operational read models.</summary>
public static class OperationalTriageAggregation
{
    public const int MaxTriageItems = 12;
    public const int MaxRecommendations = 8;
    public const int MaxAttentionItems = 8;
    public const int MaxCorrelations = 8;

    public const string RouteDashboard = "dashboard";
    public const string RouteReconciliationWorkbench = "workbench/reconciliation";
    public const string RouteInventoryWorkbench = "inventory-workbench/drift";
    public const string RouteReplayWorkbench = "replay-workbench/pressure";
    public const string RouteTrendSummary = "trends/summary";
    public const string RouteTimeline = "timeline";

    public const string CorrelationReplayTrend = "ReplayInstabilityTrendDegradation";
    public const string CorrelationDriftReconciliation = "InventoryDriftReconciliationEscalation";
    public const string CorrelationProtectiveSaturation = "ProtectiveModeSaturation";
    public const string CorrelationRecoveryStabilization = "RuntimeRecoveryStabilization";
    public const string CorrelationDriftAfterReplay = "DriftEscalationAfterReplayPressure";

    public static OperationalTriageQueueDto ComposeQueue(
        OperationalNavigationIndexDto navigation,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTrendSummaryDto trend,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated)
    {
        var items = ComposeItems(
            navigation,
            timeline,
            trend,
            dashboard,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSaturationIndicated);
        var attention = ComposeAttentionItems(items, navigation, timeline);
        var correlations = ComposeCorrelations(
            items,
            timelineCorrelations,
            replayPressure,
            replayStabilization,
            trend,
            runtimeSaturationIndicated);
        var overallPriority = items.Count > 0
            ? items.Min(i => i.PriorityBand)
            : OperationalTriagePriority.Stable;

        return new OperationalTriageQueueDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ItemCount = items.Count,
            MaxItems = MaxTriageItems,
            OverallPriority = overallPriority,
            Summary = DescribeQueueSummary(items, overallPriority),
            Items = items,
            AttentionItems = attention,
            Correlations = correlations
        };
    }

    public static IReadOnlyList<OperationalTriageRecommendationDto> ComposeRecommendations(
        OperationalNavigationIndexDto navigation,
        OperationalTimelineDto timeline,
        OperationalTrendSummaryDto trend,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var items = new List<(int Priority, OperationalTriageRecommendationDto Item)>();

        if (replayPressure.InstabilityLevel == OperationalReplayPressureLevel.Critical
            || replayStabilization.ReplayPressureEscalating)
        {
            items.Add((1, new OperationalTriageRecommendationDto
            {
                Priority = 1,
                Title = "Replay instability requires investigation",
                RecommendedAction = "Review replay stabilization workbench",
                RecommendedRoute = RouteReplayWorkbench,
                PriorityBand = OperationalTriagePriority.Critical
            }));
        }

        if (protectiveModeActive && trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((2, new OperationalTriageRecommendationDto
            {
                Priority = 2,
                Title = "Protective mode with degrading trend",
                RecommendedAction = "Review dashboard pressure and trend summary",
                RecommendedRoute = RouteDashboard,
                PriorityBand = OperationalTriagePriority.High
            }));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High)
        {
            items.Add((3, new OperationalTriageRecommendationDto
            {
                Priority = 3,
                Title = "Inventory drift escalation detected",
                RecommendedAction = "Inspect inventory drift workbench",
                RecommendedRoute = RouteInventoryWorkbench,
                PriorityBand = OperationalTriagePriority.High
            }));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || reconciliationWorkbench.Queue.UnresolvedConflicts >= 3)
        {
            items.Add((4, new OperationalTriageRecommendationDto
            {
                Priority = 4,
                Title = "Review reconciliation backlog",
                RecommendedAction = "Inspect reconciliation workbench queue",
                RecommendedRoute = RouteReconciliationWorkbench,
                PriorityBand = OperationalTriagePriority.Elevated
            }));
        }

        if (runtimeSaturationIndicated && !replayStabilization.ReplayRecoveryImproving)
        {
            items.Add((5, new OperationalTriageRecommendationDto
            {
                Priority = 5,
                Title = "Runtime saturation requires monitoring",
                RecommendedAction = "Review dashboard runtime protection indicators",
                RecommendedRoute = RouteDashboard,
                PriorityBand = OperationalTriagePriority.Elevated
            }));
        }

        if (replayStabilization.ReplayRecoveryImproving || trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            items.Add((6, new OperationalTriageRecommendationDto
            {
                Priority = 6,
                Title = "Operational conditions improving",
                RecommendedAction = "Runtime pressure stabilizing — continue monitoring",
                RecommendedRoute = RouteTrendSummary,
                PriorityBand = OperationalTriagePriority.Monitoring
            }));
        }

        foreach (var recommendation in navigation.Recommendations.Take(3))
        {
            items.Add((20 + recommendation.Priority, new OperationalTriageRecommendationDto
            {
                Priority = 20 + recommendation.Priority,
                Title = recommendation.Title,
                RecommendedAction = recommendation.RecommendedAction,
                RecommendedRoute = recommendation.RelativeRoute,
                PriorityBand = MapNavigationPriority(recommendation.Severity)
            }));
        }

        if (items.Count == 0)
        {
            items.Add((90, new OperationalTriageRecommendationDto
            {
                Priority = 90,
                Title = "Routine monitoring",
                RecommendedAction = "Operational conditions stable — continue routine monitoring",
                RecommendedRoute = RouteDashboard,
                PriorityBand = OperationalTriagePriority.Stable
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxRecommendations)
            .Select(i => i.Item)
            .ToList();
    }

    public static IReadOnlyList<OperationalTriageItemDto> ComposeItems(
        OperationalNavigationIndexDto navigation,
        OperationalTimelineDto timeline,
        OperationalTrendSummaryDto trend,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated)
    {
        var items = new List<(int Priority, OperationalTriageItemDto Item)>();

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High
            || replayStabilization.ReplayPressureEscalating)
        {
            items.Add((1, BuildItem(
                1,
                OperationalTriagePriority.Critical,
                OperationalTriageCategory.ReplayInstability,
                OperationalTriageState.InvestigationRequired,
                "Replay instability requires investigation",
                RouteReplayWorkbench,
                replayPressure.Summary,
                new[] { replayPressure.Summary, trend.Summary },
                "Review replay stabilization workbench")));
        }

        if ((dashboard.Pressure.ProtectiveModeActive || replayPressure.ProtectiveModeVisible)
            && trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((2, BuildItem(
                2,
                OperationalTriagePriority.High,
                OperationalTriageCategory.RuntimeProtection,
                OperationalTriageState.Protective,
                "Protective mode active with degrading trend",
                RouteDashboard,
                dashboard.Pressure.Summary,
                new[] { trend.Summary, dashboard.Pressure.Summary },
                "Review dashboard pressure indicators")));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High)
        {
            items.Add((3, BuildItem(
                3,
                OperationalTriagePriority.High,
                OperationalTriageCategory.InventoryDrift,
                OperationalTriageState.InvestigationRequired,
                "Inventory drift escalation detected",
                RouteInventoryWorkbench,
                inventoryWorkbench.DriftSummary.Summary,
                new[] { inventoryWorkbench.DriftSummary.Summary, reconciliationWorkbench.Queue.Summary },
                "Inspect inventory drift workbench")));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || reconciliationWorkbench.Queue.UnresolvedConflicts > 0)
        {
            items.Add((4, BuildItem(
                4,
                OperationalTriagePriority.Elevated,
                OperationalTriageCategory.ReconciliationBacklog,
                reconciliationWorkbench.Queue.EscalatingConflicts > 0
                    ? OperationalTriageState.InvestigationRequired
                    : OperationalTriageState.Monitoring,
                "Review reconciliation backlog",
                RouteReconciliationWorkbench,
                reconciliationWorkbench.Queue.Summary,
                new[] { reconciliationWorkbench.Queue.Summary, reconciliationWorkbench.ReplayRisk.Summary },
                "Inspect reconciliation workbench queue")));
        }

        if (runtimeSaturationIndicated)
        {
            items.Add((5, BuildItem(
                5,
                OperationalTriagePriority.Elevated,
                OperationalTriageCategory.RuntimeProtection,
                OperationalTriageState.Monitoring,
                "Runtime saturation indicated",
                RouteDashboard,
                "Runtime saturation requires operator monitoring",
                new[] { dashboard.Pressure.Summary },
                "Review dashboard runtime protection indicators")));
        }

        if (replayStabilization.ReplayRecoveryImproving || trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            items.Add((6, BuildItem(
                6,
                OperationalTriagePriority.Monitoring,
                OperationalTriageCategory.Stabilization,
                OperationalTriageState.Improving,
                "Runtime pressure stabilizing",
                RouteReplayWorkbench,
                replayStabilization.Summary,
                new[] { replayStabilization.Summary, trend.Summary },
                "Continue monitoring stabilization indicators")));
        }

        if (trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((7, BuildItem(
                7,
                OperationalTriagePriority.Elevated,
                OperationalTriageCategory.TrendMovement,
                OperationalTriageState.InvestigationRequired,
                "Short-window trend degrading",
                RouteTrendSummary,
                trend.Summary,
                new[] { trend.Summary },
                "Review operational trend summary")));
        }

        foreach (var timelineEvent in timeline.Events
                     .Where(e => e.Direction == OperationalTimelineDirection.Degrading
                         || e.Direction == OperationalTimelineDirection.Activated)
                     .Take(3))
        {
            items.Add((10 + RankTimelineCategory(timelineEvent.Category), BuildItem(
                10 + RankTimelineCategory(timelineEvent.Category),
                MapTimelineSeverity(timelineEvent.Severity),
                MapTimelineCategory(timelineEvent.Category),
                OperationalTriageState.InvestigationRequired,
                timelineEvent.Summary,
                timelineEvent.SuggestedRoute,
                timelineEvent.CorrelationLabel,
                new[] { timelineEvent.Summary, timelineEvent.CorrelationLabel },
                $"Inspect {timelineEvent.SuggestedRoute}")));
        }

        foreach (var attention in navigation.AttentionItems.Take(2))
        {
            items.Add((30 + attention.Priority, BuildItem(
                30 + attention.Priority,
                MapNavigationPriority(attention.Severity),
                MapNavigationSection(attention.Title),
                MapNavigationState(attention.State),
                attention.Title,
                attention.RelativeRoute,
                attention.Detail,
                new[] { attention.Detail },
                attention.Detail)));
        }

        if (items.Count == 0)
        {
            items.Add((100, BuildItem(
                100,
                OperationalTriagePriority.Stable,
                OperationalTriageCategory.SystemMonitoring,
                OperationalTriageState.Stable,
                "Operational conditions stable",
                RouteDashboard,
                navigation.Summary,
                new[] { "No urgent investigation required" },
                "Continue routine monitoring")));
        }

        return items
            .GroupBy(i => i.Item.Category)
            .Select(g => g.OrderBy(x => x.Priority).First())
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Summary, StringComparer.Ordinal)
            .Take(MaxTriageItems)
            .Select(i => i.Item)
            .ToList();
    }

    public static IReadOnlyList<OperationalTriageCorrelationDto> ComposeCorrelations(
        IReadOnlyList<OperationalTriageItemDto> items,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trend,
        bool runtimeSaturationIndicated)
    {
        var correlations = new List<(int Priority, OperationalTriageCorrelationDto Item)>();

        if (items.Any(i => i.Category == OperationalTriageCategory.ReplayInstability)
            && trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            correlations.Add((1, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = CorrelationReplayTrend,
                Summary = "Replay instability correlated with degrading short-window trend",
                Priority = OperationalTriagePriority.Critical,
                RelatedCategories = new[]
                {
                    nameof(OperationalTriageCategory.ReplayInstability),
                    nameof(OperationalTriageCategory.TrendMovement)
                },
                RecommendedRoute = RouteReplayWorkbench
            }));
        }

        if (items.Any(i => i.Category == OperationalTriageCategory.InventoryDrift)
            && items.Any(i => i.Category == OperationalTriageCategory.ReconciliationBacklog))
        {
            correlations.Add((2, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = CorrelationDriftReconciliation,
                Summary = "Inventory drift escalation correlated with reconciliation backlog pressure",
                Priority = OperationalTriagePriority.High,
                RelatedCategories = new[]
                {
                    nameof(OperationalTriageCategory.InventoryDrift),
                    nameof(OperationalTriageCategory.ReconciliationBacklog)
                },
                RecommendedRoute = RouteInventoryWorkbench
            }));
        }

        if (items.Any(i => i.Category == OperationalTriageCategory.RuntimeProtection && i.State == OperationalTriageState.Protective)
            && runtimeSaturationIndicated)
        {
            correlations.Add((3, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = CorrelationProtectiveSaturation,
                Summary = "Protective mode correlated with runtime saturation indicators",
                Priority = OperationalTriagePriority.High,
                RelatedCategories = new[] { nameof(OperationalTriageCategory.RuntimeProtection) },
                RecommendedRoute = RouteDashboard
            }));
        }

        if (replayStabilization.ReplayRecoveryImproving
            && trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            correlations.Add((4, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = CorrelationRecoveryStabilization,
                Summary = "Runtime recovery correlated with improving stabilization trend",
                Priority = OperationalTriagePriority.Monitoring,
                RelatedCategories = new[]
                {
                    nameof(OperationalTriageCategory.Stabilization),
                    nameof(OperationalTriageCategory.TrendMovement)
                },
                RecommendedRoute = RouteTrendSummary
            }));
        }

        if (items.Any(i => i.Category == OperationalTriageCategory.ReplayInstability)
            && items.Any(i => i.Category == OperationalTriageCategory.InventoryDrift))
        {
            correlations.Add((5, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = CorrelationDriftAfterReplay,
                Summary = "Drift escalation after replay pressure observed in triage queue",
                Priority = OperationalTriagePriority.Elevated,
                RelatedCategories = new[]
                {
                    nameof(OperationalTriageCategory.ReplayInstability),
                    nameof(OperationalTriageCategory.InventoryDrift)
                },
                RecommendedRoute = RouteInventoryWorkbench
            }));
        }

        foreach (var timelineCorrelation in timelineCorrelations.Take(3))
        {
            correlations.Add((20, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = timelineCorrelation.CorrelationLabel,
                Summary = timelineCorrelation.Summary,
                Priority = MapTimelineSeverity(timelineCorrelation.Severity),
                RelatedCategories = timelineCorrelation.RelatedCategories.ToList(),
                RecommendedRoute = timelineCorrelation.SuggestedRoute
            }));
        }

        if (correlations.Count == 0)
        {
            correlations.Add((50, new OperationalTriageCorrelationDto
            {
                CorrelationLabel = "StableMonitoring",
                Summary = "No correlated degradation patterns detected in current triage queue",
                Priority = OperationalTriagePriority.Stable,
                RelatedCategories = new[] { nameof(OperationalTriageCategory.SystemMonitoring) },
                RecommendedRoute = RouteDashboard
            }));
        }

        return correlations
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Item.CorrelationLabel, StringComparer.Ordinal)
            .Take(MaxCorrelations)
            .Select(c => c.Item)
            .ToList();
    }

    public static IReadOnlyList<OperationalTriageAttentionDto> ComposeAttentionItems(
        IReadOnlyList<OperationalTriageItemDto> items,
        OperationalNavigationIndexDto navigation,
        OperationalTimelineDto timeline)
    {
        var attention = items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Summary, StringComparer.Ordinal)
            .Take(MaxAttentionItems - 1)
            .Select(i => new OperationalTriageAttentionDto
            {
                Priority = i.Priority,
                PriorityBand = i.PriorityBand,
                Category = i.Category,
                Title = i.Summary,
                Detail = i.InvestigationReason,
                RecommendedRoute = i.RecommendedRoute
            })
            .ToList();

        if (attention.Count == 0)
        {
            attention.Add(new OperationalTriageAttentionDto
            {
                Priority = 100,
                PriorityBand = OperationalTriagePriority.Stable,
                Category = OperationalTriageCategory.SystemMonitoring,
                Title = "Triage queue monitoring active",
                Detail = navigation.Summary,
                RecommendedRoute = RouteDashboard
            });
        }

        if (timeline.Events.Count > 0 && attention.Count < MaxAttentionItems)
        {
            var latest = timeline.Events[^1];
            attention.Add(new OperationalTriageAttentionDto
            {
                Priority = 90,
                PriorityBand = MapTimelineSeverity(latest.Severity),
                Category = MapTimelineCategory(latest.Category),
                Title = $"Latest timeline: {latest.Summary}",
                Detail = timeline.Summary,
                RecommendedRoute = RouteTimeline
            });
        }

        return attention
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .ToList();
    }

    private static OperationalTriageItemDto BuildItem(
        int priority,
        OperationalTriagePriority priorityBand,
        OperationalTriageCategory category,
        OperationalTriageState state,
        string summary,
        string route,
        string investigationReason,
        IReadOnlyList<string> correlatedSignals,
        string suggestedAction) =>
        new()
        {
            Priority = priority,
            PriorityBand = priorityBand,
            Category = category,
            State = state,
            Summary = summary,
            RecommendedRoute = route,
            InvestigationReason = investigationReason,
            CorrelatedSignals = correlatedSignals
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .Take(4)
                .ToList(),
            SuggestedOperatorAction = suggestedAction
        };

    private static string DescribeQueueSummary(
        IReadOnlyList<OperationalTriageItemDto> items,
        OperationalTriagePriority overallPriority)
    {
        if (items.Count == 0)
            return "No investigation items in the current triage queue.";

        var top = items.OrderBy(i => i.Priority).First();
        return overallPriority <= OperationalTriagePriority.High
            ? $"Investigate first: {top.Summary}. {top.SuggestedOperatorAction}."
            : $"Triage queue available ({overallPriority} overall priority). Top item: {top.Summary}.";
    }

    private static OperationalTriagePriority MapNavigationPriority(OperationalNavigationSeverity severity) =>
        severity switch
        {
            OperationalNavigationSeverity.Critical => OperationalTriagePriority.Critical,
            OperationalNavigationSeverity.High => OperationalTriagePriority.High,
            OperationalNavigationSeverity.Elevated => OperationalTriagePriority.Elevated,
            OperationalNavigationSeverity.Moderate => OperationalTriagePriority.Moderate,
            _ => OperationalTriagePriority.Stable
        };

    private static OperationalTriagePriority MapTimelineSeverity(OperationalTimelineSeverity severity) =>
        severity switch
        {
            OperationalTimelineSeverity.Critical => OperationalTriagePriority.Critical,
            OperationalTimelineSeverity.High => OperationalTriagePriority.High,
            OperationalTimelineSeverity.Elevated => OperationalTriagePriority.Elevated,
            OperationalTimelineSeverity.Moderate => OperationalTriagePriority.Moderate,
            _ => OperationalTriagePriority.Stable
        };

    private static OperationalTriageState MapNavigationState(OperationalNavigationState state) =>
        state switch
        {
            OperationalNavigationState.ActionNeeded => OperationalTriageState.InvestigationRequired,
            OperationalNavigationState.Protective => OperationalTriageState.Protective,
            OperationalNavigationState.Monitoring => OperationalTriageState.Monitoring,
            OperationalNavigationState.Stable => OperationalTriageState.Stable,
            _ => OperationalTriageState.Monitoring
        };

    private static OperationalTriageCategory MapNavigationSection(string title)
    {
        if (title.Contains("Replay", StringComparison.OrdinalIgnoreCase))
            return OperationalTriageCategory.ReplayInstability;
        if (title.Contains("Inventory", StringComparison.OrdinalIgnoreCase))
            return OperationalTriageCategory.InventoryDrift;
        if (title.Contains("Reconciliation", StringComparison.OrdinalIgnoreCase))
            return OperationalTriageCategory.ReconciliationBacklog;
        if (title.Contains("Protective", StringComparison.OrdinalIgnoreCase))
            return OperationalTriageCategory.RuntimeProtection;
        if (title.Contains("Trend", StringComparison.OrdinalIgnoreCase))
            return OperationalTriageCategory.TrendMovement;
        return OperationalTriageCategory.SystemMonitoring;
    }

    private static OperationalTriageCategory MapTimelineCategory(OperationalTimelineCategory category) =>
        category switch
        {
            OperationalTimelineCategory.ReplayPressure => OperationalTriageCategory.ReplayInstability,
            OperationalTimelineCategory.RuntimeProtection => OperationalTriageCategory.RuntimeProtection,
            OperationalTimelineCategory.InventoryDrift => OperationalTriageCategory.InventoryDrift,
            OperationalTimelineCategory.ReconciliationPressure => OperationalTriageCategory.ReconciliationBacklog,
            OperationalTimelineCategory.Stabilization => OperationalTriageCategory.Stabilization,
            OperationalTimelineCategory.TrendMovement => OperationalTriageCategory.TrendMovement,
            _ => OperationalTriageCategory.SystemMonitoring
        };

    private static int RankTimelineCategory(OperationalTimelineCategory category) => category switch
    {
        OperationalTimelineCategory.ReplayPressure => 1,
        OperationalTimelineCategory.RuntimeProtection => 2,
        OperationalTimelineCategory.InventoryDrift => 3,
        OperationalTimelineCategory.ReconciliationPressure => 4,
        _ => 5
    };
}
