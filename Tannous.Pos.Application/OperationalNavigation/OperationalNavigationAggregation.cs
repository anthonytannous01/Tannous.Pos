using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Deterministic operator navigation index from existing operational read models.</summary>
public static class OperationalNavigationAggregation
{
    public const int MaxRecommendations = 8;
    public const int MaxAttentionItems = 8;

    public const string RouteDashboard = "dashboard";
    public const string RouteReconciliationWorkbench = "workbench/reconciliation";
    public const string RouteInventoryWorkbench = "inventory-workbench/drift";
    public const string RouteReplayWorkbench = "replay-workbench/pressure";
    public const string RouteTrendSummary = "trends/summary";

    public const string SectionSystemHealth = "System Health";
    public const string SectionReplayStability = "Replay Stability";
    public const string SectionInventoryDrift = "Inventory Drift";
    public const string SectionReconciliationPressure = "Reconciliation Pressure";
    public const string SectionRuntimeProtection = "Runtime Protection";
    public const string SectionTrendStability = "Trend Stability";

    public static OperationalNavigationIndexDto ComposeIndex(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalNavigationReadinessSignals readinessSignals,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection)
    {
        var sections = ComposeSections(
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            replayPressure,
            replayStabilization,
            trendSummary,
            readinessSignals,
            runtimeProtection);
        var recommendations = ComposeRecommendations(
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            replayPressure,
            replayStabilization,
            trendSummary,
            readinessSignals);
        var attentionItems = ComposeAttentionItems(
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            replayPressure,
            replayStabilization,
            trendSummary,
            readinessSignals);
        var overallSeverity = ResolveOverallSeverity(sections, recommendations);
        var overallState = ResolveOverallState(sections, recommendations, readinessSignals);

        return new OperationalNavigationIndexDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            OverallSeverity = overallSeverity,
            OverallState = overallState,
            Summary = DescribeOverallSummary(overallSeverity, overallState, recommendations),
            Sections = sections,
            Recommendations = recommendations,
            AttentionItems = attentionItems
        };
    }

    public static IReadOnlyList<OperationalNavigationRouteDto> ComposeRoutes(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalNavigationReadinessSignals readinessSignals)
    {
        var routes = new List<OperationalNavigationRouteDto>
        {
            BuildRoute(
                "Operational Dashboard",
                RouteDashboard,
                MapDashboardSeverity(dashboard),
                MapDashboardState(dashboard),
                dashboard.Health.Summary),
            BuildRoute(
                "Replay Stabilization Workbench",
                RouteReplayWorkbench,
                MapReplaySeverity(replayPressure),
                MapReplayState(replayPressure, replayStabilization),
                replayPressure.Summary),
            BuildRoute(
                "Inventory Drift Workbench",
                RouteInventoryWorkbench,
                MapInventorySeverity(inventoryWorkbench),
                MapInventoryState(inventoryWorkbench),
                inventoryWorkbench.DriftSummary.Summary),
            BuildRoute(
                "Reconciliation Workbench",
                RouteReconciliationWorkbench,
                MapReconciliationSeverity(reconciliationWorkbench),
                MapReconciliationState(reconciliationWorkbench),
                reconciliationWorkbench.Queue.Summary),
            BuildRoute(
                "Operational Trend Summary",
                RouteTrendSummary,
                MapTrendSeverity(trendSummary),
                MapTrendState(trendSummary),
                trendSummary.Summary)
        };

        if (readinessSignals.RuntimeProtectionActive || dashboard.Pressure.ProtectiveModeActive)
        {
            routes.Add(BuildRoute(
                "Runtime Protection Overview",
                RouteDashboard,
                OperationalNavigationSeverity.High,
                OperationalNavigationState.Protective,
                "Protective runtime conditions are active — review dashboard pressure indicators."));
        }

        return routes
            .OrderByDescending(r => r.Severity)
            .ThenBy(r => r.DisplayName, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<OperationalNavigationSectionDto> ComposeSections(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalNavigationReadinessSignals readinessSignals,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection)
    {
        return new[]
        {
            new OperationalNavigationSectionDto
            {
                SectionName = SectionSystemHealth,
                Severity = MapDashboardSeverity(dashboard),
                State = MapDashboardState(dashboard),
                RecommendedRoute = RouteDashboard,
                RecommendedAction = dashboard.Health.State >= OperationalDashboardHealthState.Degraded
                    ? "Review operational dashboard health indicators"
                    : "Monitor operational dashboard",
                AttentionSummary = dashboard.Health.Summary
            },
            new OperationalNavigationSectionDto
            {
                SectionName = SectionReplayStability,
                Severity = MapReplaySeverity(replayPressure),
                State = MapReplayState(replayPressure, replayStabilization),
                RecommendedRoute = RouteReplayWorkbench,
                RecommendedAction = replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High
                    ? "Review replay stabilization workbench"
                    : "Monitor replay pressure indicators",
                AttentionSummary = replayPressure.Summary
            },
            new OperationalNavigationSectionDto
            {
                SectionName = SectionInventoryDrift,
                Severity = MapInventorySeverity(inventoryWorkbench),
                State = MapInventoryState(inventoryWorkbench),
                RecommendedRoute = RouteInventoryWorkbench,
                RecommendedAction = inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High
                    ? "Inventory drift requires operator review"
                    : "Monitor inventory drift indicators",
                AttentionSummary = inventoryWorkbench.DriftSummary.Summary
            },
            new OperationalNavigationSectionDto
            {
                SectionName = SectionReconciliationPressure,
                Severity = MapReconciliationSeverity(reconciliationWorkbench),
                State = MapReconciliationState(reconciliationWorkbench),
                RecommendedRoute = RouteReconciliationWorkbench,
                RecommendedAction = reconciliationWorkbench.Queue.EscalatingConflicts > 0
                    ? "Inspect reconciliation backlog"
                    : "Monitor reconciliation queue",
                AttentionSummary = reconciliationWorkbench.Queue.Summary
            },
            new OperationalNavigationSectionDto
            {
                SectionName = SectionRuntimeProtection,
                Severity = MapRuntimeSeverity(readinessSignals, runtimeProtection, dashboard),
                State = MapRuntimeState(readinessSignals, runtimeProtection, dashboard),
                RecommendedRoute = RouteDashboard,
                RecommendedAction = readinessSignals.RuntimeProtectionActive
                    ? "Review runtime protection indicators on dashboard"
                    : "Runtime protection nominal",
                AttentionSummary = DescribeRuntimeSummary(readinessSignals, runtimeProtection, dashboard)
            },
            new OperationalNavigationSectionDto
            {
                SectionName = SectionTrendStability,
                Severity = MapTrendSeverity(trendSummary),
                State = MapTrendState(trendSummary),
                RecommendedRoute = RouteTrendSummary,
                RecommendedAction = trendSummary.OverallDirection == OperationalTrendDirection.Degrading
                    ? "Review short-window operational trend summary"
                    : "Operational state stable",
                AttentionSummary = trendSummary.Summary
            }
        };
    }

    public static IReadOnlyList<OperationalNavigationRecommendationDto> ComposeRecommendations(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalNavigationReadinessSignals readinessSignals)
    {
        var items = new List<(int Priority, OperationalNavigationRecommendationDto Item)>();

        if (replayPressure.InstabilityLevel == OperationalReplayPressureLevel.Critical
            || replayStabilization.ReplayPressureEscalating)
        {
            items.Add((1, new OperationalNavigationRecommendationDto
            {
                Priority = 1,
                Title = "Critical replay instability",
                RecommendedAction = "Review replay stabilization workbench",
                RelativeRoute = RouteReplayWorkbench,
                Severity = OperationalNavigationSeverity.Critical
            }));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity == OperationalInventoryDriftSeverity.Critical
            || inventoryWorkbench.DriftSummary.EscalatingDriftConflicts >= 3)
        {
            items.Add((2, new OperationalNavigationRecommendationDto
            {
                Priority = 2,
                Title = "Severe inventory drift",
                RecommendedAction = "Inventory drift requires operator review",
                RelativeRoute = RouteInventoryWorkbench,
                Severity = OperationalNavigationSeverity.Critical
            }));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || reconciliationWorkbench.ReplayRisk.ReplayEscalationObserved)
        {
            items.Add((3, new OperationalNavigationRecommendationDto
            {
                Priority = 3,
                Title = "Reconciliation escalation",
                RecommendedAction = "Inspect reconciliation backlog",
                RelativeRoute = RouteReconciliationWorkbench,
                Severity = OperationalNavigationSeverity.High
            }));
        }

        if (readinessSignals.RuntimeProtectionActive
            || dashboard.Pressure.ProtectiveModeActive
            || replayPressure.ProtectiveModeVisible)
        {
            items.Add((4, new OperationalNavigationRecommendationDto
            {
                Priority = 4,
                Title = "Runtime protective mode",
                RecommendedAction = "Review runtime protection indicators on dashboard",
                RelativeRoute = RouteDashboard,
                Severity = OperationalNavigationSeverity.High
            }));
        }

        if (trendSummary.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((5, new OperationalNavigationRecommendationDto
            {
                Priority = 5,
                Title = "Trend degradation",
                RecommendedAction = "Review short-window operational trend summary",
                RelativeRoute = RouteTrendSummary,
                Severity = MapTrendSeverity(trendSummary)
            }));
        }

        if (replayPressure.InstabilityLevel == OperationalReplayPressureLevel.High
            && !items.Any(i => i.Priority == 1))
        {
            items.Add((6, new OperationalNavigationRecommendationDto
            {
                Priority = 6,
                Title = "Elevated replay pressure",
                RecommendedAction = "Review replay stabilization workbench",
                RelativeRoute = RouteReplayWorkbench,
                Severity = OperationalNavigationSeverity.Elevated
            }));
        }

        if (items.Count == 0)
        {
            items.Add((50, new OperationalNavigationRecommendationDto
            {
                Priority = 50,
                Title = "Stable monitoring",
                RecommendedAction = "Operational state stable — continue routine monitoring",
                RelativeRoute = RouteDashboard,
                Severity = OperationalNavigationSeverity.Nominal
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxRecommendations)
            .Select(i => i.Item)
            .ToList();
    }

    public static IReadOnlyList<OperationalNavigationAttentionDto> ComposeAttentionItems(
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trendSummary,
        OperationalNavigationReadinessSignals readinessSignals)
    {
        var items = new List<(int Priority, OperationalNavigationAttentionDto Item)>();

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High)
        {
            items.Add((1, new OperationalNavigationAttentionDto
            {
                Priority = 1,
                Severity = MapReplaySeverity(replayPressure),
                State = MapReplayState(replayPressure, replayStabilization),
                Title = "Replay instability elevated",
                Detail = replayPressure.Summary,
                RelativeRoute = RouteReplayWorkbench
            }));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High)
        {
            items.Add((2, new OperationalNavigationAttentionDto
            {
                Priority = 2,
                Severity = MapInventorySeverity(inventoryWorkbench),
                State = MapInventoryState(inventoryWorkbench),
                Title = "Inventory drift attention required",
                Detail = inventoryWorkbench.DriftSummary.Summary,
                RelativeRoute = RouteInventoryWorkbench
            }));
        }

        if (reconciliationWorkbench.Queue.UnresolvedConflicts > 0)
        {
            items.Add((3, new OperationalNavigationAttentionDto
            {
                Priority = 3,
                Severity = MapReconciliationSeverity(reconciliationWorkbench),
                State = MapReconciliationState(reconciliationWorkbench),
                Title = "Reconciliation backlog visible",
                Detail = reconciliationWorkbench.Queue.Summary,
                RelativeRoute = RouteReconciliationWorkbench
            }));
        }

        if (readinessSignals.RuntimeProtectionActive || dashboard.Pressure.ProtectiveModeActive)
        {
            items.Add((4, new OperationalNavigationAttentionDto
            {
                Priority = 4,
                Severity = OperationalNavigationSeverity.High,
                State = OperationalNavigationState.Protective,
                Title = "Protective mode active",
                Detail = "Runtime protection or protective containment is currently indicated.",
                RelativeRoute = RouteDashboard
            }));
        }

        if (trendSummary.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((5, new OperationalNavigationAttentionDto
            {
                Priority = 5,
                Severity = MapTrendSeverity(trendSummary),
                State = OperationalNavigationState.ActionNeeded,
                Title = "Short-window trend degrading",
                Detail = trendSummary.Summary,
                RelativeRoute = RouteTrendSummary
            }));
        }

        foreach (var trendAttention in trendSummary.AttentionItems.Take(2))
        {
            items.Add((10 + trendAttention.Priority, new OperationalNavigationAttentionDto
            {
                Priority = 10 + trendAttention.Priority,
                Severity = MapTrendSeverity(trendSummary),
                State = trendAttention.Direction == OperationalTrendDirection.Degrading
                    ? OperationalNavigationState.ActionNeeded
                    : OperationalNavigationState.Monitoring,
                Title = trendAttention.Title,
                Detail = trendAttention.Detail,
                RelativeRoute = RouteTrendSummary
            }));
        }

        if (items.Count == 0)
        {
            items.Add((90, new OperationalNavigationAttentionDto
            {
                Priority = 90,
                Severity = OperationalNavigationSeverity.Nominal,
                State = OperationalNavigationState.Stable,
                Title = "No urgent operational attention",
                Detail = "Operational state stable across monitored domains.",
                RelativeRoute = RouteDashboard
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static OperationalNavigationRouteDto BuildRoute(
        string displayName,
        string relativeRoute,
        OperationalNavigationSeverity severity,
        OperationalNavigationState attentionState,
        string summary) =>
        new()
        {
            DisplayName = displayName,
            RelativeRoute = relativeRoute,
            Severity = severity,
            AttentionState = attentionState,
            OperatorSummary = summary
        };

    private static OperationalNavigationSeverity ResolveOverallSeverity(
        IReadOnlyList<OperationalNavigationSectionDto> sections,
        IReadOnlyList<OperationalNavigationRecommendationDto> recommendations)
    {
        var maxSection = sections.Count > 0 ? (int)sections.Max(s => s.Severity) : 0;
        var maxRecommendation = recommendations.Count > 0 ? (int)recommendations.Max(r => r.Severity) : 0;
        return (OperationalNavigationSeverity)Math.Max(maxSection, maxRecommendation);
    }

    private static OperationalNavigationState ResolveOverallState(
        IReadOnlyList<OperationalNavigationSectionDto> sections,
        IReadOnlyList<OperationalNavigationRecommendationDto> recommendations,
        OperationalNavigationReadinessSignals readinessSignals)
    {
        if (readinessSignals.RuntimeProtectionActive)
            return OperationalNavigationState.Protective;

        if (recommendations.Any(r => r.Priority <= 3))
            return OperationalNavigationState.ActionNeeded;

        if (sections.Any(s => s.State == OperationalNavigationState.Degraded))
            return OperationalNavigationState.Degraded;

        if (sections.Any(s => s.State == OperationalNavigationState.AttentionRequired))
            return OperationalNavigationState.AttentionRequired;

        return OperationalNavigationState.Stable;
    }

    private static string DescribeOverallSummary(
        OperationalNavigationSeverity severity,
        OperationalNavigationState state,
        IReadOnlyList<OperationalNavigationRecommendationDto> recommendations)
    {
        var top = recommendations.OrderBy(r => r.Priority).FirstOrDefault();
        if (top is not null && top.Priority <= 5)
            return $"{top.Title}: {top.RecommendedAction}.";

        return state switch
        {
            OperationalNavigationState.Protective => "Protective runtime conditions active — review recommended routes for detail.",
            OperationalNavigationState.ActionNeeded => "Operational attention recommended — inspect prioritized navigation routes.",
            OperationalNavigationState.Stable => "Operational state stable — routine monitoring recommended.",
            _ => $"Operational navigation index available ({severity} severity)."
        };
    }

    private static OperationalNavigationSeverity MapDashboardSeverity(OperationalDashboardSummaryDto dashboard) =>
        dashboard.Health.State switch
        {
            OperationalDashboardHealthState.Critical => OperationalNavigationSeverity.Critical,
            OperationalDashboardHealthState.Degraded => OperationalNavigationSeverity.High,
            OperationalDashboardHealthState.AttentionRequired => OperationalNavigationSeverity.Elevated,
            _ => OperationalNavigationSeverity.Nominal
        };

    private static OperationalNavigationState MapDashboardState(OperationalDashboardSummaryDto dashboard) =>
        dashboard.Health.AttentionState switch
        {
            OperationalDashboardAttentionState.Urgent => OperationalNavigationState.ActionNeeded,
            OperationalDashboardAttentionState.ActionNeeded => OperationalNavigationState.ActionNeeded,
            OperationalDashboardAttentionState.Monitoring => OperationalNavigationState.Monitoring,
            _ => dashboard.Health.State == OperationalDashboardHealthState.Healthy
                ? OperationalNavigationState.Stable
                : OperationalNavigationState.AttentionRequired
        };

    private static OperationalNavigationSeverity MapReplaySeverity(OperationalReplayPressureSummaryDto replay) =>
        replay.InstabilityLevel switch
        {
            OperationalReplayPressureLevel.Critical => OperationalNavigationSeverity.Critical,
            OperationalReplayPressureLevel.High => OperationalNavigationSeverity.High,
            OperationalReplayPressureLevel.Elevated => OperationalNavigationSeverity.Elevated,
            _ => OperationalNavigationSeverity.Nominal
        };

    private static OperationalNavigationState MapReplayState(
        OperationalReplayPressureSummaryDto replay,
        OperationalReplayStabilizationDto stabilization)
    {
        if (replay.ProtectiveModeVisible || stabilization.ProtectiveContainmentActive)
            return OperationalNavigationState.Protective;
        if (replay.InstabilityLevel >= OperationalReplayPressureLevel.High)
            return OperationalNavigationState.ActionNeeded;
        if (stabilization.StabilizationActive)
            return OperationalNavigationState.Monitoring;
        return OperationalNavigationState.Stable;
    }

    private static OperationalNavigationSeverity MapInventorySeverity(OperationalInventoryWorkbenchDto inventory) =>
        inventory.DriftSummary.DriftSeverity switch
        {
            OperationalInventoryDriftSeverity.Critical => OperationalNavigationSeverity.Critical,
            OperationalInventoryDriftSeverity.High => OperationalNavigationSeverity.High,
            OperationalInventoryDriftSeverity.Elevated => OperationalNavigationSeverity.Elevated,
            _ => OperationalNavigationSeverity.Nominal
        };

    private static OperationalNavigationState MapInventoryState(OperationalInventoryWorkbenchDto inventory)
    {
        if (inventory.DriftSummary.ProtectiveModeActive)
            return OperationalNavigationState.Protective;
        if (inventory.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High)
            return OperationalNavigationState.ActionNeeded;
        if (inventory.DriftSummary.TotalInventoryDriftConflicts > 0)
            return OperationalNavigationState.AttentionRequired;
        return OperationalNavigationState.Stable;
    }

    private static OperationalNavigationSeverity MapReconciliationSeverity(OperationalReconciliationWorkbenchDto workbench)
    {
        if (workbench.Queue.EscalatingConflicts >= 3)
            return OperationalNavigationSeverity.Critical;
        if (workbench.Queue.EscalatingConflicts > 0 || workbench.ReplayRisk.ReplayEscalationObserved)
            return OperationalNavigationSeverity.High;
        if (workbench.Queue.UnresolvedConflicts > 0)
            return OperationalNavigationSeverity.Elevated;
        return OperationalNavigationSeverity.Nominal;
    }

    private static OperationalNavigationState MapReconciliationState(OperationalReconciliationWorkbenchDto workbench)
    {
        if (workbench.ReplayRisk.ProtectiveModeActive)
            return OperationalNavigationState.Protective;
        if (workbench.Queue.EscalatingConflicts > 0)
            return OperationalNavigationState.ActionNeeded;
        if (workbench.Queue.UnresolvedConflicts > 0)
            return OperationalNavigationState.AttentionRequired;
        return OperationalNavigationState.Stable;
    }

    private static OperationalNavigationSeverity MapRuntimeSeverity(
        OperationalNavigationReadinessSignals readiness,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalDashboardSummaryDto dashboard)
    {
        if (readiness.RuntimeProtectionActive || runtimeProtection.FailsafeActive || dashboard.Pressure.ProtectiveModeActive)
            return OperationalNavigationSeverity.High;
        if (string.Equals(readiness.PressureSeverity, "High", StringComparison.OrdinalIgnoreCase)
            || string.Equals(readiness.PressureSeverity, "Critical", StringComparison.OrdinalIgnoreCase))
            return OperationalNavigationSeverity.Elevated;
        return OperationalNavigationSeverity.Nominal;
    }

    private static OperationalNavigationState MapRuntimeState(
        OperationalNavigationReadinessSignals readiness,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalDashboardSummaryDto dashboard)
    {
        if (readiness.RuntimeProtectionActive || runtimeProtection.FailsafeActive || dashboard.Pressure.ProtectiveModeActive)
            return OperationalNavigationState.Protective;
        if (dashboard.Pressure.RuntimeSaturationIndicated)
            return OperationalNavigationState.AttentionRequired;
        return OperationalNavigationState.Stable;
    }

    private static OperationalNavigationSeverity MapTrendSeverity(OperationalTrendSummaryDto trend) =>
        trend.Severity switch
        {
            OperationalTrendSeverity.Critical => OperationalNavigationSeverity.Critical,
            OperationalTrendSeverity.High => OperationalNavigationSeverity.High,
            OperationalTrendSeverity.Elevated => OperationalNavigationSeverity.Elevated,
            OperationalTrendSeverity.Moderate => OperationalNavigationSeverity.Moderate,
            _ => OperationalNavigationSeverity.Nominal
        };

    private static OperationalNavigationState MapTrendState(OperationalTrendSummaryDto trend) =>
        trend.OverallDirection switch
        {
            OperationalTrendDirection.Degrading => OperationalNavigationState.ActionNeeded,
            OperationalTrendDirection.Improving => OperationalNavigationState.Monitoring,
            _ => OperationalNavigationState.Stable
        };

    private static string DescribeRuntimeSummary(
        OperationalNavigationReadinessSignals readiness,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalDashboardSummaryDto dashboard)
    {
        if (readiness.RuntimeProtectionActive || runtimeProtection.FailsafeActive)
            return "Runtime protection is active — review dashboard pressure indicators.";
        if (dashboard.Pressure.ProtectiveModeActive)
            return "Protective mode is indicated on the operational dashboard.";
        return string.IsNullOrWhiteSpace(readiness.ReadinessState)
            ? "Runtime protection indicators are nominal."
            : $"Runtime readiness: {readiness.ReadinessState}.";
    }
}
