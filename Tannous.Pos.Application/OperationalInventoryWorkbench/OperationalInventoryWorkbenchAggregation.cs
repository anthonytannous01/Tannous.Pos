using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Deterministic operator inventory drift workbench composition from existing diagnostics summaries.</summary>
public static class OperationalInventoryWorkbenchAggregation
{
    public const int MaxHotspots = 5;
    public const int MaxAttentionItems = 8;

    public const string HotspotReplayLinkedInventory = "ReplayLinkedInventoryConflicts";
    public const string HotspotStaleReconciliation = "StaleReconciliationPressure";
    public const string HotspotExportPressure = "ExportPressureImpact";
    public const string HotspotRepeatedDriftEscalation = "RepeatedDriftEscalation";
    public const string HotspotAlertLinkedInstability = "AlertLinkedInventoryInstability";
    public const string HotspotCascadingDegradation = "CascadingDegradationVisibility";

    public const string CategoryInventoryCountMismatch = "InventoryCountMismatch";
    public const string CategoryReplayReconciliationMismatch = "ReplayReconciliationMismatch";
    public const string CategorySynchronizationInstability = "SynchronizationInstability";
    public const string CategoryStaleOperationalVisibility = "StaleOperationalVisibility";
    public const string CategoryCascadingInventoryDegradation = "CascadingInventoryDegradation";

    public static OperationalInventoryDriftSummaryDto ComposeDriftSummary(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalIncidentSummaryDto incidents,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var totalDrift = reconciliation.InventoryDriftRiskCount;
        var unresolved = reconciliation.UnresolvedCount > 0 && totalDrift > 0
            ? Math.Min(reconciliation.UnresolvedCount, totalDrift)
            : totalDrift;
        var escalating = reconciliation.InvestigatingCount + incidents.CascadingDegradationCount;
        var replayLinked = reconciliation.ReplayMismatchCount
            + alerts.ReplayRelatedSignals
            + (resilience.ReplayStormRiskIndicated ? 1 : 0);
        var protectiveMode = dashboard.Pressure.ProtectiveModeActive
            || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive;
        var severity = ClassifyDriftSeverity(totalDrift, escalating, replayLinked, alerts, incidents);

        return new OperationalInventoryDriftSummaryDto
        {
            TotalInventoryDriftConflicts = totalDrift,
            UnresolvedDriftConflicts = unresolved,
            EscalatingDriftConflicts = escalating,
            ReplayLinkedDriftPressure = replayLinked,
            ProtectiveModeActive = protectiveMode,
            DriftSeverity = severity,
            Summary = DescribeDriftSummary(severity, totalDrift, unresolved, protectiveMode)
        };
    }

    public static IReadOnlyList<OperationalInventoryDriftHotspotDto> ComposeHotspots(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalIncidentSummaryDto incidents,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var candidates = new List<OperationalInventoryDriftHotspotDto>();

        var replayLinkedCount = reconciliation.InventoryDriftRiskCount
            + reconciliation.ReplayMismatchCount
            + alerts.ReplayRelatedSignals;
        if (replayLinkedCount > 0 || resilience.ReplayStormRiskIndicated)
        {
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotReplayLinkedInventory,
                Source = "ReconciliationAndResilience",
                Severity = ClassifyCountSeverity(
                    Math.Max(replayLinkedCount, resilience.ReplayStormRiskIndicated ? 1 : 0),
                    1, 3, 6),
                PressureCount = Math.Max(replayLinkedCount, resilience.ReplayStormRiskIndicated ? 1 : 0),
                Summary = "Replay-linked inventory conflicts are contributing to drift pressure."
            });
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0 || reconciliation.UnresolvedCount > 0)
        {
            var pressure = staleCount + (reconciliation.UnresolvedCount > 0 ? 1 : 0);
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotStaleReconciliation,
                Source = "DiagnosticsFreshnessAndReconciliation",
                Severity = ClassifyCountSeverity(pressure, 1, 3, 6),
                PressureCount = pressure,
                Summary = "Stale reconciliation visibility may reduce inventory drift confidence."
            });
        }

        if (resilience.ExportTruncationPressureIndicated || dashboard.Pressure.ExportPressureIndicated)
        {
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotExportPressure,
                Source = "ResilienceAndDashboard",
                Severity = reconciliation.InventoryDriftRiskCount >= 3
                    ? OperationalInventoryDriftSeverity.High
                    : OperationalInventoryDriftSeverity.Elevated,
                PressureCount = Math.Max(reconciliation.InventoryDriftRiskCount, 1),
                Summary = "Export pressure may limit forensic visibility during inventory drift review."
            });
        }

        var escalationCount = reconciliation.InvestigatingCount
            + reconciliationWorkbench.Queue.EscalatingConflicts;
        if (escalationCount > 0 || reconciliation.InventoryDriftRiskCount > 1)
        {
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotRepeatedDriftEscalation,
                Source = "ReconciliationWorkbench",
                Severity = ClassifyCountSeverity(escalationCount + reconciliation.InventoryDriftRiskCount, 1, 3, 5),
                PressureCount = escalationCount + reconciliation.InventoryDriftRiskCount,
                Summary = "Repeated drift escalation signals require operator attention."
            });
        }

        var alertLinked = alerts.InventoryRelatedSignals + alerts.ReplayRelatedSignals;
        if (alertLinked > 0)
        {
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotAlertLinkedInstability,
                Source = "AlertSignals",
                Severity = alerts.CriticalSignals > 0
                    ? OperationalInventoryDriftSeverity.Critical
                    : ClassifyCountSeverity(alertLinked, 1, 2, 4),
                PressureCount = alertLinked,
                Summary = "Alert-linked inventory instability is active."
            });
        }

        if (incidents.CascadingDegradationCount > 0 || reconciliationWorkbench.Queue.EscalatingConflicts > 0)
        {
            var cascadeCount = incidents.CascadingDegradationCount + reconciliationWorkbench.Queue.EscalatingConflicts;
            candidates.Add(new OperationalInventoryDriftHotspotDto
            {
                Category = HotspotCascadingDegradation,
                Source = "IncidentCorrelation",
                Severity = ClassifyCountSeverity(cascadeCount, 1, 2, 4),
                PressureCount = cascadeCount,
                Summary = "Cascading degradation may amplify inventory drift impact."
            });
        }

        return candidates
            .OrderByDescending(h => h.Severity)
            .ThenByDescending(h => h.PressureCount)
            .ThenBy(h => h.Category, StringComparer.Ordinal)
            .Take(MaxHotspots)
            .ToList();
    }

    public static OperationalInventoryResolutionReadinessDto ComposeResolutionReadiness(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var blockedByReplay = resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount > 0
            || reconciliationWorkbench.ReplayRisk.ReplayEscalationObserved;
        var blockedByProtective = dashboard.Pressure.ProtectiveModeActive
            || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive;
        var stabilizing = reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            || reconciliation.InvestigatingCount > 0;
        var manualRecommended = reconciliation.InventoryDriftRiskCount > 0
            || alerts.InventoryRelatedSignals > 0
            || reconciliationWorkbench.InventoryDrift.ManualReviewRecommended;
        var readyForReview = reconciliation.InventoryDriftRiskCount > 0
            && !blockedByProtective
            && !blockedByReplay;

        var state = ClassifyResolutionState(
            readyForReview,
            stabilizing,
            blockedByReplay,
            blockedByProtective,
            manualRecommended);

        return new OperationalInventoryResolutionReadinessDto
        {
            ResolutionState = state,
            ReadyForOperatorReview = readyForReview,
            StabilizationInProgress = stabilizing && !blockedByProtective,
            BlockedByReplayPressure = blockedByReplay,
            BlockedByProtectiveMode = blockedByProtective,
            ManualReconciliationRecommended = manualRecommended,
            Summary = DescribeResolutionReadiness(state, readyForReview, blockedByReplay, blockedByProtective, manualRecommended)
        };
    }

    public static IReadOnlyList<OperationalInventoryMismatchCategoryDto> ComposeMismatchCategories(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalIncidentSummaryDto incidents,
        OperationalCacheGovernanceOverviewDto governanceOverview)
    {
        var categories = new List<OperationalInventoryMismatchCategoryDto>();

        if (reconciliation.InventoryDriftRiskCount > 0)
        {
            categories.Add(new OperationalInventoryMismatchCategoryDto
            {
                Category = CategoryInventoryCountMismatch,
                ConflictCount = reconciliation.InventoryDriftRiskCount,
                Severity = ClassifyCountSeverity(reconciliation.InventoryDriftRiskCount, 1, 3, 6),
                Summary = "Inventory count mismatches detected in reconciliation conflicts."
            });
        }

        if (reconciliation.ReplayMismatchCount > 0 || alerts.ReplayRelatedSignals > 0)
        {
            var count = reconciliation.ReplayMismatchCount + alerts.ReplayRelatedSignals;
            categories.Add(new OperationalInventoryMismatchCategoryDto
            {
                Category = CategoryReplayReconciliationMismatch,
                ConflictCount = count,
                Severity = ClassifyCountSeverity(count, 1, 2, 4),
                Summary = "Replay reconciliation mismatches may affect inventory alignment."
            });
        }

        if (resilience.ReplayStormRiskIndicated || alerts.InventoryRelatedSignals > 0)
        {
            var count = (resilience.ReplayStormRiskIndicated ? 1 : 0) + alerts.InventoryRelatedSignals;
            categories.Add(new OperationalInventoryMismatchCategoryDto
            {
                Category = CategorySynchronizationInstability,
                ConflictCount = count,
                Severity = resilience.ReplayStormRiskIndicated
                    ? OperationalInventoryDriftSeverity.High
                    : ClassifyCountSeverity(count, 1, 2, 3),
                Summary = "Synchronization instability signals may correlate with inventory drift."
            });
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0)
        {
            categories.Add(new OperationalInventoryMismatchCategoryDto
            {
                Category = CategoryStaleOperationalVisibility,
                ConflictCount = staleCount,
                Severity = ClassifyCountSeverity(staleCount, 1, 3, 5),
                Summary = "Stale operational visibility may reduce drift assessment confidence."
            });
        }

        if (incidents.CascadingDegradationCount > 0)
        {
            categories.Add(new OperationalInventoryMismatchCategoryDto
            {
                Category = CategoryCascadingInventoryDegradation,
                ConflictCount = incidents.CascadingDegradationCount,
                Severity = ClassifyCountSeverity(incidents.CascadingDegradationCount, 1, 2, 4),
                Summary = "Cascading inventory degradation incidents are active."
            });
        }

        return categories
            .OrderByDescending(c => c.Severity)
            .ThenByDescending(c => c.ConflictCount)
            .ThenBy(c => c.Category, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<OperationalInventoryAttentionItemDto> ComposeAttentionItems(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalIncidentSummaryDto incidents,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var items = new List<(int Priority, OperationalInventoryAttentionItemDto Item)>();

        if (reconciliation.InventoryDriftRiskCount >= 3
            || (reconciliation.InventoryDriftRiskCount > 0 && alerts.CriticalSignals > 0))
        {
            items.Add((1, CreateAttentionItem(
                1,
                OperationalInventoryDriftSeverity.Critical,
                "Review critical inventory drift backlog",
                $"Critical inventory drift: {reconciliation.InventoryDriftRiskCount} conflict(s) require review.")));
        }
        else if (reconciliation.InventoryDriftRiskCount > 0)
        {
            items.Add((1, CreateAttentionItem(
                1,
                OperationalInventoryDriftSeverity.Elevated,
                "Review inventory drift backlog",
                $"Inventory drift conflicts: {reconciliation.InventoryDriftRiskCount} require operator review.")));
        }

        if (resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount > 0
            || reconciliationWorkbench.ReplayRisk.ReplayEscalationObserved)
        {
            items.Add((2, CreateAttentionItem(
                2,
                OperationalInventoryDriftSeverity.High,
                "Investigate replay-linked inventory instability",
                "Replay pressure may be amplifying inventory drift — verify sync and replay activity.")));
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0 || reconciliation.UnresolvedCount > 0)
        {
            items.Add((3, CreateAttentionItem(
                3,
                ClassifyCountSeverity(staleCount + reconciliation.UnresolvedCount, 1, 3, 6),
                "Reduce stale reconciliation pressure",
                "Stale diagnostics or unresolved reconciliation may reduce drift visibility confidence.")));
        }

        if (alerts.InventoryRelatedSignals > 0 || alerts.CriticalSignals > 0)
        {
            items.Add((4, CreateAttentionItem(
                4,
                alerts.CriticalSignals > 0
                    ? OperationalInventoryDriftSeverity.Critical
                    : OperationalInventoryDriftSeverity.Elevated,
                "Review alert-linked drift escalation",
                $"Alert signals linked to inventory: {alerts.InventoryRelatedSignals} inventory, {alerts.CriticalSignals} critical.")));
        }

        if (dashboard.Pressure.ProtectiveModeActive || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive)
        {
            items.Add((5, CreateAttentionItem(
                5,
                OperationalInventoryDriftSeverity.High,
                "Stabilize reconciliation visibility",
                "Protective mode is active — inventory drift detail may be reduced until load stabilizes.")));
        }

        if (incidents.CascadingDegradationCount > 0)
        {
            items.Add((6, CreateAttentionItem(
                6,
                ClassifyCountSeverity(incidents.CascadingDegradationCount, 1, 2, 4),
                "Investigate cascading degradation hotspots",
                $"{incidents.CascadingDegradationCount} cascading degradation signal(s) may affect inventory stability.")));
        }

        if (resilience.ExportTruncationPressureIndicated || dashboard.Pressure.ExportPressureIndicated)
        {
            items.Add((7, CreateAttentionItem(
                7,
                OperationalInventoryDriftSeverity.Elevated,
                "Review export pressure impact on drift review",
                "Export truncation pressure may limit forensic context during inventory drift resolution.")));
        }

        if (reconciliationWorkbench.InventoryDrift.ManualReviewRecommended)
        {
            var severity = reconciliationWorkbench.InventoryDrift.DriftSeverity
                is OperationalWorkbench.OperationalWorkbenchSeverity.High
                or OperationalWorkbench.OperationalWorkbenchSeverity.Critical
                    ? OperationalInventoryDriftSeverity.High
                    : OperationalInventoryDriftSeverity.Elevated;
            items.Add((8, CreateAttentionItem(
                8,
                severity,
                "Manual reconciliation recommended",
                "Reconciliation workbench indicates manual inventory review is recommended.")));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static OperationalInventoryAttentionItemDto CreateAttentionItem(
        int priority,
        OperationalInventoryDriftSeverity severity,
        string title,
        string guidance) =>
        new()
        {
            Priority = priority,
            Severity = severity,
            Title = title,
            Guidance = guidance
        };

    private static OperationalInventoryDriftSeverity ClassifyDriftSeverity(
        int totalDrift,
        int escalating,
        int replayLinked,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents)
    {
        if (totalDrift >= 6 || (totalDrift >= 3 && alerts.CriticalSignals > 0))
            return OperationalInventoryDriftSeverity.Critical;
        if (totalDrift >= 3 || escalating >= 3 || incidents.CascadingDegradationCount >= 2)
            return OperationalInventoryDriftSeverity.High;
        if (totalDrift >= 1 || replayLinked >= 2 || alerts.InventoryRelatedSignals > 0)
            return OperationalInventoryDriftSeverity.Elevated;

        return OperationalInventoryDriftSeverity.Nominal;
    }

    private static OperationalInventoryDriftSeverity ClassifyCountSeverity(
        int count,
        int elevatedThreshold,
        int highThreshold,
        int criticalThreshold)
    {
        if (count >= criticalThreshold)
            return OperationalInventoryDriftSeverity.Critical;
        if (count >= highThreshold)
            return OperationalInventoryDriftSeverity.High;
        if (count >= elevatedThreshold)
            return OperationalInventoryDriftSeverity.Elevated;

        return count > 0 ? OperationalInventoryDriftSeverity.Elevated : OperationalInventoryDriftSeverity.Nominal;
    }

    private static OperationalInventoryResolutionState ClassifyResolutionState(
        bool readyForReview,
        bool stabilizing,
        bool blockedByReplay,
        bool blockedByProtective,
        bool manualRecommended)
    {
        if (blockedByProtective)
            return OperationalInventoryResolutionState.BlockedByProtectiveMode;
        if (blockedByReplay)
            return OperationalInventoryResolutionState.BlockedByReplayPressure;
        if (manualRecommended && readyForReview)
            return OperationalInventoryResolutionState.ManualReconciliationRecommended;
        if (stabilizing)
            return OperationalInventoryResolutionState.StabilizationInProgress;
        if (readyForReview)
            return OperationalInventoryResolutionState.ReadyForOperatorReview;

        return OperationalInventoryResolutionState.StabilizationInProgress;
    }

    private static string DescribeDriftSummary(
        OperationalInventoryDriftSeverity severity,
        int totalDrift,
        int unresolved,
        bool protectiveMode)
    {
        if (totalDrift == 0)
            return "Nominal — no inventory drift conflicts detected.";

        var mode = protectiveMode ? " Protective mode is active." : string.Empty;
        return $"{severity} inventory drift — {totalDrift} total conflict(s), {unresolved} unresolved.{mode}";
    }

    private static string DescribeResolutionReadiness(
        OperationalInventoryResolutionState state,
        bool readyForReview,
        bool blockedByReplay,
        bool blockedByProtective,
        bool manualRecommended)
    {
        return state switch
        {
            OperationalInventoryResolutionState.BlockedByProtectiveMode =>
                "Resolution visibility blocked by protective mode — wait for load stabilization.",
            OperationalInventoryResolutionState.BlockedByReplayPressure =>
                "Resolution blocked by replay pressure — stabilize replay activity before drift resolution.",
            OperationalInventoryResolutionState.ManualReconciliationRecommended =>
                "Manual reconciliation recommended — operator review is the next step.",
            OperationalInventoryResolutionState.ReadyForOperatorReview =>
                "Ready for operator review — inventory drift conflicts can be triaged.",
            OperationalInventoryResolutionState.StabilizationInProgress =>
                readyForReview || manualRecommended
                    ? "Stabilization in progress — monitor before committing to resolution."
                    : "Stabilization in progress — no immediate drift action required.",
            _ => blockedByReplay || blockedByProtective
                ? "Resolution readiness is constrained by active operational pressure."
                : "Resolution readiness is advisory only."
        };
    }
}
