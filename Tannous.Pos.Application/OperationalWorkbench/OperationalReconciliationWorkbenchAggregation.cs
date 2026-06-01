using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Deterministic operator reconciliation workbench composition from existing diagnostics summaries.</summary>
public static class OperationalReconciliationWorkbenchAggregation
{
    public const int MaxHotspots = 5;
    public const int MaxAttentionItems = 8;

    public const string HotspotReplayPressure = "ReplayPressure";
    public const string HotspotInventoryDrift = "InventoryDrift";
    public const string HotspotRepeatedInvalidations = "RepeatedInvalidations";
    public const string HotspotStaleDiagnosticsPressure = "StaleDiagnosticsPressure";
    public const string HotspotExportPressure = "ExportPressure";

    public static OperationalReconciliationQueueDto ComposeQueue(
        ReconciliationSummaryDto reconciliation,
        OperationalIncidentSummaryDto incidents)
    {
        var escalating = reconciliation.InvestigatingCount + incidents.CascadingDegradationCount;
        var active = reconciliation.UnresolvedCount + reconciliation.InvestigatingCount;

        return new OperationalReconciliationQueueDto
        {
            ActiveConflicts = active,
            UnresolvedConflicts = reconciliation.UnresolvedCount,
            ReplayRiskConflicts = reconciliation.ReplayMismatchCount,
            InventoryDriftConflicts = reconciliation.InventoryDriftRiskCount,
            EscalatingConflicts = escalating,
            Summary = DescribeQueue(active, reconciliation.UnresolvedCount, escalating)
        };
    }

    public static IReadOnlyList<OperationalReconciliationHotspotDto> ComposeHotspots(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard)
    {
        var candidates = new List<OperationalReconciliationHotspotDto>();

        var replayCount = reconciliation.ReplayMismatchCount
            + alerts.ReplayRelatedSignals
            + (resilience.ReplayStormRiskIndicated ? 1 : 0);
        if (replayCount > 0 || resilience.ReplayStormRiskIndicated)
        {
            candidates.Add(new OperationalReconciliationHotspotDto
            {
                Category = HotspotReplayPressure,
                Source = "ReconciliationAndResilience",
                Severity = ClassifyReplayHotspotSeverity(replayCount, resilience.ReplayStormRiskIndicated),
                PressureCount = Math.Max(replayCount, resilience.ReplayStormRiskIndicated ? 1 : 0),
                Summary = "Replay pressure is contributing to reconciliation workload."
            });
        }

        var driftCount = reconciliation.InventoryDriftRiskCount + alerts.InventoryRelatedSignals;
        if (driftCount > 0)
        {
            candidates.Add(new OperationalReconciliationHotspotDto
            {
                Category = HotspotInventoryDrift,
                Source = "ReconciliationAndAlerts",
                Severity = ClassifyCountSeverity(driftCount, elevatedThreshold: 2, highThreshold: 5, criticalThreshold: 10),
                PressureCount = driftCount,
                Summary = "Inventory drift conflicts require reconciliation review."
            });
        }

        var invalidationCount = (int)Math.Min(int.MaxValue, governanceOverview.TotalInvalidations);
        if (invalidationCount >= 3)
        {
            candidates.Add(new OperationalReconciliationHotspotDto
            {
                Category = HotspotRepeatedInvalidations,
                Source = "DiagnosticsCache",
                Severity = ClassifyCountSeverity(invalidationCount, elevatedThreshold: 5, highThreshold: 15, criticalThreshold: 30),
                PressureCount = invalidationCount,
                Summary = "Repeated diagnostics invalidations are increasing reconciliation read churn."
            });
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0)
        {
            candidates.Add(new OperationalReconciliationHotspotDto
            {
                Category = HotspotStaleDiagnosticsPressure,
                Source = "DiagnosticsFreshness",
                Severity = ClassifyCountSeverity(staleCount, elevatedThreshold: 1, highThreshold: 3, criticalThreshold: 6),
                PressureCount = staleCount,
                Summary = "Stale diagnostics may reduce confidence in reconciliation summaries."
            });
        }

        if (resilience.ExportTruncationPressureIndicated || dashboard.Pressure.ExportPressureIndicated)
        {
            var exportCount = (resilience.ExportTruncationPressureIndicated ? 1 : 0)
                + (dashboard.Pressure.ExportPressureIndicated ? 1 : 0)
                + (resilience.ExportTruncationPressureIndicated && reconciliation.UnresolvedCount > 0 ? reconciliation.UnresolvedCount : 0);
            candidates.Add(new OperationalReconciliationHotspotDto
            {
                Category = HotspotExportPressure,
                Source = "ResilienceAndDashboard",
                Severity = ClassifyExportHotspotSeverity(resilience, reconciliation),
                PressureCount = Math.Max(exportCount, 1),
                Summary = "Export volume pressure may affect reconciliation forensic visibility."
            });
        }

        return candidates
            .OrderByDescending(h => h.Severity)
            .ThenByDescending(h => h.PressureCount)
            .ThenBy(h => h.Category, StringComparer.Ordinal)
            .Take(MaxHotspots)
            .ToList();
    }

    public static OperationalReconciliationReplayRiskDto ComposeReplayRisk(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalIncidentSummaryDto incidents,
        OperationalDashboardSummaryDto dashboard)
    {
        var escalationObserved = resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount > 0
            || incidents.ReplayIncidentCount > 0;
        var protectiveMode = dashboard.Pressure.ProtectiveModeActive;
        var instability = ClassifyReplayInstability(resilience, reconciliation, incidents);
        var recovering = !resilience.ReplayStormRiskIndicated
            && !protectiveMode
            && reconciliation.InvestigatingCount > 0
            && reconciliation.ReplayMismatchCount == 0;

        return new OperationalReconciliationReplayRiskDto
        {
            InstabilityLevel = instability,
            ProtectiveModeActive = protectiveMode,
            ReplayEscalationObserved = escalationObserved,
            StabilizationRecovering = recovering,
            Summary = DescribeReplayRisk(instability, protectiveMode, escalationObserved, recovering)
        };
    }

    public static OperationalReconciliationInventoryDriftDto ComposeInventoryDrift(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts)
    {
        var mismatchCount = reconciliation.InventoryDriftRiskCount;
        var severity = ClassifyCountSeverity(
            mismatchCount + alerts.InventoryRelatedSignals,
            elevatedThreshold: 1,
            highThreshold: 3,
            criticalThreshold: 6);
        var attention = ClassifyDriftAttention(severity, mismatchCount, alerts.InventoryRelatedSignals);
        var manualReview = mismatchCount > 0 || alerts.InventoryRelatedSignals > 0;

        return new OperationalReconciliationInventoryDriftDto
        {
            DriftSeverity = severity,
            ActiveInventoryMismatchCount = mismatchCount,
            AttentionState = attention,
            ManualReviewRecommended = manualReview,
            Summary = DescribeInventoryDrift(mismatchCount, severity, manualReview)
        };
    }

    public static IReadOnlyList<OperationalReconciliationAttentionItemDto> ComposeAttentionItems(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalResilienceSummaryDto resilience,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard)
    {
        var items = new List<(int Priority, OperationalReconciliationAttentionItemDto Item)>();

        if (reconciliation.UnresolvedCount > 0)
        {
            items.Add((1, CreateAttentionItem(
                1,
                OperationalWorkbenchAttentionState.ActionNeeded,
                ClassifyCountSeverity(reconciliation.UnresolvedCount, 3, 8, 15),
                "Review unresolved reconciliation backlog",
                $"Triage {reconciliation.UnresolvedCount} unresolved conflict(s) in the reconciliation queue.")));
        }

        if (resilience.ReplayStormRiskIndicated || reconciliation.ReplayMismatchCount > 0 || incidents.ReplayIncidentCount > 0)
        {
            items.Add((2, CreateAttentionItem(
                2,
                OperationalWorkbenchAttentionState.ActionNeeded,
                ClassifyReplayHotspotSeverity(
                    reconciliation.ReplayMismatchCount + incidents.ReplayIncidentCount,
                    resilience.ReplayStormRiskIndicated),
                "Investigate replay escalation",
                "Replay instability signals are present — verify device sync and replay activity.")));
        }

        if (reconciliation.InventoryDriftRiskCount > 0 || alerts.InventoryRelatedSignals > 0)
        {
            items.Add((3, CreateAttentionItem(
                3,
                OperationalWorkbenchAttentionState.ActionNeeded,
                ClassifyCountSeverity(
                    reconciliation.InventoryDriftRiskCount + alerts.InventoryRelatedSignals,
                    1, 3, 6),
                "Resolve inventory drift conflicts",
                "Inventory mismatch conflicts require manual stock reconciliation review.")));
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0)
        {
            items.Add((4, CreateAttentionItem(
                4,
                OperationalWorkbenchAttentionState.Monitoring,
                ClassifyCountSeverity(staleCount, 1, 3, 6),
                "Refresh stale diagnostics",
                "Diagnostics freshness indicators suggest summaries may need refresh before decisions.")));
        }

        if (resilience.ExportTruncationPressureIndicated || dashboard.Pressure.ExportPressureIndicated)
        {
            items.Add((5, CreateAttentionItem(
                5,
                OperationalWorkbenchAttentionState.Monitoring,
                OperationalWorkbenchSeverity.Elevated,
                "Reduce export pressure",
                "Export volume pressure may truncate forensic visibility during reconciliation review.")));
        }

        if (alerts.CriticalSignals > 0 || alerts.WarningSignals > 0)
        {
            items.Add((6, CreateAttentionItem(
                6,
                alerts.CriticalSignals > 0
                    ? OperationalWorkbenchAttentionState.Urgent
                    : OperationalWorkbenchAttentionState.Monitoring,
                alerts.CriticalSignals > 0
                    ? OperationalWorkbenchSeverity.High
                    : OperationalWorkbenchSeverity.Moderate,
                "Review active alert conditions",
                $"Active alert signals: {alerts.TotalSignals} total ({alerts.CriticalSignals} critical).")));
        }

        var escalating = reconciliation.InvestigatingCount + incidents.CascadingDegradationCount;
        if (escalating > 0)
        {
            items.Add((7, CreateAttentionItem(
                7,
                OperationalWorkbenchAttentionState.ActionNeeded,
                ClassifyCountSeverity(escalating, 1, 3, 6),
                "Review escalating reconciliation conflicts",
                $"{escalating} conflict(s) are under active escalation or cascading degradation.")));
        }

        if (incidents.HighRiskIncidentCount > 0 || incidents.CriticalIncidentCount > 0)
        {
            items.Add((8, CreateAttentionItem(
                8,
                incidents.CriticalIncidentCount > 0
                    ? OperationalWorkbenchAttentionState.Urgent
                    : OperationalWorkbenchAttentionState.ActionNeeded,
                incidents.CriticalIncidentCount > 0
                    ? OperationalWorkbenchSeverity.Critical
                    : OperationalWorkbenchSeverity.High,
                "Correlate incidents with reconciliation backlog",
                "Correlated incidents may indicate systemic reconciliation pressure.")));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static OperationalReconciliationAttentionItemDto CreateAttentionItem(
        int priority,
        OperationalWorkbenchAttentionState attentionState,
        OperationalWorkbenchSeverity severity,
        string title,
        string guidance) =>
        new()
        {
            Priority = priority,
            AttentionState = attentionState,
            Severity = severity,
            Title = title,
            Guidance = guidance
        };

    private static OperationalWorkbenchSeverity ClassifyReplayHotspotSeverity(int count, bool replayStormRisk)
    {
        if (replayStormRisk && count >= 3)
            return OperationalWorkbenchSeverity.Critical;
        if (replayStormRisk || count >= 5)
            return OperationalWorkbenchSeverity.High;
        if (count >= 2)
            return OperationalWorkbenchSeverity.Elevated;
        if (count >= 1)
            return OperationalWorkbenchSeverity.Moderate;

        return OperationalWorkbenchSeverity.Low;
    }

    private static OperationalWorkbenchSeverity ClassifyExportHotspotSeverity(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation)
    {
        if (resilience.ExportTruncationPressureIndicated && reconciliation.UnresolvedCount >= 5)
            return OperationalWorkbenchSeverity.High;
        if (resilience.ExportTruncationPressureIndicated)
            return OperationalWorkbenchSeverity.Elevated;

        return OperationalWorkbenchSeverity.Moderate;
    }

    private static OperationalWorkbenchSeverity ClassifyCountSeverity(
        int count,
        int elevatedThreshold,
        int highThreshold,
        int criticalThreshold)
    {
        if (count >= criticalThreshold)
            return OperationalWorkbenchSeverity.Critical;
        if (count >= highThreshold)
            return OperationalWorkbenchSeverity.High;
        if (count >= elevatedThreshold)
            return OperationalWorkbenchSeverity.Elevated;
        if (count >= 1)
            return OperationalWorkbenchSeverity.Moderate;

        return OperationalWorkbenchSeverity.Low;
    }

    private static OperationalWorkbenchAttentionState ClassifyDriftAttention(
        OperationalWorkbenchSeverity severity,
        int mismatchCount,
        int inventoryAlertSignals)
    {
        if (severity >= OperationalWorkbenchSeverity.High || inventoryAlertSignals > 0)
            return OperationalWorkbenchAttentionState.Urgent;
        if (severity >= OperationalWorkbenchSeverity.Elevated || mismatchCount > 0)
            return OperationalWorkbenchAttentionState.ActionNeeded;
        if (severity >= OperationalWorkbenchSeverity.Moderate)
            return OperationalWorkbenchAttentionState.Monitoring;

        return OperationalWorkbenchAttentionState.Normal;
    }

    private static string ClassifyReplayInstability(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalIncidentSummaryDto incidents)
    {
        if (resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount >= 5
            || incidents.ReplayIncidentCount >= 3)
            return "High replay instability";

        if (resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount > 0
            || incidents.ReplayIncidentCount > 0
            || string.Equals(resilience.ReconciliationBacklogSeverity, "High", StringComparison.OrdinalIgnoreCase))
            return "Moderate replay instability";

        return "Low replay instability";
    }

    private static string DescribeQueue(int active, int unresolved, int escalating)
    {
        if (active == 0)
            return "No active reconciliation conflicts in the workbench queue.";

        return $"Active={active}, Unresolved={unresolved}, Escalating={escalating}.";
    }

    private static string DescribeReplayRisk(
        string instability,
        bool protectiveMode,
        bool escalationObserved,
        bool recovering)
    {
        if (protectiveMode)
            return $"{instability} — protective mode active; replay detail may be reduced.";

        if (recovering)
            return $"{instability} — stabilization recovering while conflicts are being investigated.";

        if (escalationObserved)
            return $"{instability} — replay escalation observed; review sync activity.";

        return $"{instability} — replay conditions appear stable.";
    }

    private static string DescribeInventoryDrift(
        int mismatchCount,
        OperationalWorkbenchSeverity severity,
        bool manualReview)
    {
        if (mismatchCount == 0 && !manualReview)
            return "No active inventory drift conflicts detected.";

        var review = manualReview ? " Manual review recommended." : string.Empty;
        return $"{severity} inventory drift — {mismatchCount} active mismatch conflict(s).{review}";
    }
}
