using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Operator runtime protection signals extracted upstream (no governance DTO exposure).</summary>
public sealed class OperationalReplayRuntimeSignals
{
    public bool ProtectiveContainmentActive { get; init; }
    public bool RuntimeSaturationIndicated { get; init; }
}

/// <summary>Deterministic operator replay pressure workbench composition from existing diagnostics summaries.</summary>
public static class OperationalReplayWorkbenchAggregation
{
    public const int MaxHotspots = 5;
    public const int MaxAttentionItems = 8;

    public const string HotspotReplayEscalation = "ReplayEscalation";
    public const string HotspotRepeatedInvalidationChurn = "RepeatedInvalidationChurn";
    public const string HotspotCascadingReplayDegradation = "CascadingReplayDegradation";
    public const string HotspotInventoryLinkedReplay = "InventoryLinkedReplayInstability";
    public const string HotspotStaleDiagnostics = "StaleDiagnosticsVisibility";
    public const string HotspotExportAmplification = "ExportPressureAmplification";
    public const string HotspotAlertLinkedReplay = "AlertLinkedReplayInstability";

    public static OperationalReplayPressureSummaryDto ComposePressureSummary(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayRuntimeSignals runtimeSignals)
    {
        var activePressure = reconciliation.ReplayMismatchCount
            + alerts.ReplayRelatedSignals
            + incidents.ReplayIncidentCount
            + (resilience.ReplayStormRiskIndicated ? 1 : 0);
        var escalationVisible = resilience.ReplayStormRiskIndicated
            || reconciliation.ReplayMismatchCount > 0
            || reconciliationWorkbench.ReplayRisk.ReplayEscalationObserved;
        var protectiveVisible = runtimeSignals.ProtectiveContainmentActive
            || dashboard.Pressure.ProtectiveModeActive
            || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive;
        var recoveryProgression = reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            || (reconciliation.InvestigatingCount > 0 && !resilience.ReplayStormRiskIndicated);
        var instability = ClassifyPressureLevel(activePressure, resilience, alerts, incidents);
        var stabilizationState = ClassifyStabilizationPressureState(
            resilience,
            reconciliation,
            reconciliationWorkbench,
            runtimeSignals,
            escalationVisible,
            recoveryProgression);

        return new OperationalReplayPressureSummaryDto
        {
            InstabilityLevel = instability,
            ActiveReplayPressure = activePressure,
            ReplayEscalationVisible = escalationVisible,
            ProtectiveModeVisible = protectiveVisible,
            RecoveryProgressionIndicated = recoveryProgression,
            StabilizationPressureState = stabilizationState,
            Summary = DescribePressureSummary(instability, activePressure, protectiveVisible, recoveryProgression)
        };
    }

    public static OperationalReplayStabilizationDto ComposeStabilization(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayRuntimeSignals runtimeSignals,
        OperationalReplayPressureSummaryDto pressureSummary)
    {
        var protective = runtimeSignals.ProtectiveContainmentActive
            || dashboard.Pressure.ProtectiveModeActive
            || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive;
        var recovering = reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            || pressureSummary.RecoveryProgressionIndicated;
        var stalled = resilience.ReplayStormRiskIndicated
            && reconciliation.InvestigatingCount == 0
            && !recovering;
        var escalating = pressureSummary.ReplayEscalationVisible
            && !recovering;
        var stabilizationActive = recovering
            || reconciliation.InvestigatingCount > 0
            || protective;
        var intervention = escalating
            || protective
            || inventoryWorkbench.DriftSummary.ProtectiveModeActive
            || (pressureSummary.InstabilityLevel >= OperationalReplayPressureLevel.High && !recovering);

        return new OperationalReplayStabilizationDto
        {
            StabilizationActive = stabilizationActive,
            ReplayRecoveryImproving = recovering,
            ReplayRecoveryStalled = stalled,
            ReplayPressureEscalating = escalating,
            ProtectiveContainmentActive = protective,
            OperatorInterventionRecommended = intervention,
            Summary = DescribeStabilization(recovering, stalled, escalating, protective, intervention)
        };
    }

    public static IReadOnlyList<OperationalReplayHotspotDto> ComposeHotspots(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench)
    {
        var candidates = new List<OperationalReplayHotspotDto>();

        var escalationCount = reconciliation.ReplayMismatchCount
            + incidents.ReplayIncidentCount
            + (resilience.ReplayStormRiskIndicated ? 2 : 0);
        if (escalationCount > 0 || resilience.ReplayStormRiskIndicated)
        {
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotReplayEscalation,
                Source = "ResilienceAndReconciliation",
                Severity = ClassifyCountSeverity(Math.Max(escalationCount, 1), 1, 3, 6),
                PressureCount = Math.Max(escalationCount, 1),
                Summary = "Replay escalation is contributing to operational instability."
            });
        }

        var invalidationCount = (int)Math.Min(int.MaxValue, governanceOverview.TotalInvalidations);
        if (invalidationCount >= 3)
        {
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotRepeatedInvalidationChurn,
                Source = "DiagnosticsCache",
                Severity = ClassifyCountSeverity(invalidationCount, 5, 15, 30),
                PressureCount = invalidationCount,
                Summary = "Repeated invalidation churn may amplify replay read pressure."
            });
        }

        if (incidents.CascadingDegradationCount > 0 || reconciliationWorkbench.Queue.EscalatingConflicts > 0)
        {
            var count = incidents.CascadingDegradationCount + reconciliationWorkbench.Queue.EscalatingConflicts;
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotCascadingReplayDegradation,
                Source = "IncidentCorrelation",
                Severity = ClassifyCountSeverity(count, 1, 2, 4),
                PressureCount = count,
                Summary = "Cascading replay degradation may spread across reconciliation workflows."
            });
        }

        var inventoryLinked = reconciliation.InventoryDriftRiskCount
            + inventoryWorkbench.DriftSummary.ReplayLinkedDriftPressure;
        if (inventoryLinked > 0)
        {
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotInventoryLinkedReplay,
                Source = "InventoryWorkbench",
                Severity = ClassifyCountSeverity(inventoryLinked, 1, 3, 5),
                PressureCount = inventoryLinked,
                Summary = "Inventory-linked replay instability is active."
            });
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0)
        {
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotStaleDiagnostics,
                Source = "DiagnosticsFreshness",
                Severity = ClassifyCountSeverity(staleCount, 1, 3, 6),
                PressureCount = staleCount,
                Summary = "Stale diagnostics visibility may reduce replay assessment confidence."
            });
        }

        if (resilience.ExportTruncationPressureIndicated || dashboard.Pressure.ExportPressureIndicated)
        {
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotExportAmplification,
                Source = "ResilienceAndDashboard",
                Severity = resilience.ReplayStormRiskIndicated
                    ? OperationalReplayPressureLevel.High
                    : OperationalReplayPressureLevel.Elevated,
                PressureCount = Math.Max(reconciliation.ReplayMismatchCount, 1),
                Summary = "Export pressure may amplify replay forensic visibility constraints."
            });
        }

        if (alerts.ReplayRelatedSignals > 0 || alerts.CriticalSignals > 0)
        {
            var count = alerts.ReplayRelatedSignals + alerts.CriticalSignals;
            candidates.Add(new OperationalReplayHotspotDto
            {
                Category = HotspotAlertLinkedReplay,
                Source = "AlertSignals",
                Severity = alerts.CriticalSignals > 0
                    ? OperationalReplayPressureLevel.Critical
                    : ClassifyCountSeverity(count, 1, 2, 4),
                PressureCount = count,
                Summary = "Alert-linked replay pressure signals are active."
            });
        }

        return candidates
            .OrderByDescending(h => h.Severity)
            .ThenByDescending(h => h.PressureCount)
            .ThenBy(h => h.Category, StringComparer.Ordinal)
            .Take(MaxHotspots)
            .ToList();
    }

    public static OperationalReplayRecoveryConfidenceDto ComposeRecoveryConfidence(
        OperationalResilienceSummaryDto resilience,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto stabilization,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalReplayRuntimeSignals runtimeSignals)
    {
        var confidence = ClassifyRecoveryConfidence(
            resilience,
            dashboard,
            reconciliationWorkbench,
            stabilization,
            governanceOverview,
            runtimeSignals);

        return new OperationalReplayRecoveryConfidenceDto
        {
            Confidence = confidence,
            Summary = DescribeRecoveryConfidence(confidence, stabilization.ReplayRecoveryImproving)
        };
    }

    public static IReadOnlyList<OperationalReplayAttentionItemDto> ComposeAttentionItems(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayStabilizationDto stabilization,
        OperationalReplayPressureSummaryDto pressureSummary)
    {
        var items = new List<(int Priority, OperationalReplayAttentionItemDto Item)>();

        if (pressureSummary.ReplayEscalationVisible || resilience.ReplayStormRiskIndicated)
        {
            items.Add((1, CreateAttentionItem(
                1,
                pressureSummary.InstabilityLevel >= OperationalReplayPressureLevel.High
                    ? OperationalReplayPressureLevel.Critical
                    : OperationalReplayPressureLevel.High,
                "Investigate escalating replay instability",
                "Replay escalation signals are active — verify sync activity and device replay patterns.")));
        }

        if (pressureSummary.ActiveReplayPressure >= 3 || incidents.ReplayIncidentCount > 0)
        {
            items.Add((2, CreateAttentionItem(
                2,
                ClassifyCountSeverity(pressureSummary.ActiveReplayPressure, 3, 6, 10),
                "Reduce replay pressure amplification",
                "Multiple replay pressure sources are active — reduce concurrent sync/export load where possible.")));
        }

        if (stabilization.ProtectiveContainmentActive || pressureSummary.ProtectiveModeVisible)
        {
            items.Add((3, CreateAttentionItem(
                3,
                OperationalReplayPressureLevel.High,
                "Review protective mode conditions",
                "Protective containment is active — operational detail may be reduced until load stabilizes.")));
        }

        var staleCount = governanceOverview.AgingEntryCount
            + governanceOverview.NearExpiryEntryCount
            + governanceOverview.ExpiredEntryCount;
        if (staleCount > 0 || reconciliation.UnresolvedCount > 0)
        {
            items.Add((4, CreateAttentionItem(
                4,
                ClassifyCountSeverity(staleCount + reconciliation.UnresolvedCount, 1, 3, 6),
                "Stabilize reconciliation visibility",
                "Stale diagnostics or unresolved reconciliation may reduce replay assessment confidence.")));
        }

        if (incidents.CascadingDegradationCount > 0 || reconciliationWorkbench.Queue.EscalatingConflicts > 0)
        {
            items.Add((5, CreateAttentionItem(
                5,
                ClassifyCountSeverity(
                    incidents.CascadingDegradationCount + reconciliationWorkbench.Queue.EscalatingConflicts,
                    1, 2, 4),
                "Resolve cascading replay degradation",
                "Cascading degradation hotspots may amplify replay instability across workflows.")));
        }

        if (inventoryWorkbench.DriftSummary.ReplayLinkedDriftPressure > 0
            || reconciliation.InventoryDriftRiskCount > 0)
        {
            items.Add((6, CreateAttentionItem(
                6,
                OperationalReplayPressureLevel.Elevated,
                "Investigate inventory-linked replay instability",
                "Inventory drift and replay pressure are correlated — review stock reconciliation context.")));
        }

        if (alerts.ReplayRelatedSignals > 0 || alerts.CriticalSignals > 0)
        {
            items.Add((7, CreateAttentionItem(
                7,
                alerts.CriticalSignals > 0
                    ? OperationalReplayPressureLevel.Critical
                    : OperationalReplayPressureLevel.Elevated,
                "Review alert-linked replay pressure",
                $"Active alert signals: {alerts.ReplayRelatedSignals} replay-related, {alerts.CriticalSignals} critical.")));
        }

        if (stabilization.OperatorInterventionRecommended && stabilization.ReplayRecoveryStalled)
        {
            items.Add((8, CreateAttentionItem(
                8,
                OperationalReplayPressureLevel.High,
                "Operator intervention recommended for stalled recovery",
                "Replay recovery appears stalled — manual review of sync and reconciliation state is advised.")));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static OperationalReplayAttentionItemDto CreateAttentionItem(
        int priority,
        OperationalReplayPressureLevel severity,
        string title,
        string guidance) =>
        new()
        {
            Priority = priority,
            Severity = severity,
            Title = title,
            Guidance = guidance
        };

    private static OperationalReplayPressureLevel ClassifyPressureLevel(
        int activePressure,
        OperationalResilienceSummaryDto resilience,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents)
    {
        if (resilience.ReplayStormRiskIndicated && activePressure >= 3)
            return OperationalReplayPressureLevel.Critical;
        if (resilience.ReplayStormRiskIndicated || activePressure >= 5 || incidents.ReplayIncidentCount >= 3)
            return OperationalReplayPressureLevel.High;
        if (activePressure >= 2 || alerts.ReplayRelatedSignals > 0 || incidents.ReplayIncidentCount > 0)
            return OperationalReplayPressureLevel.Elevated;

        return activePressure > 0
            ? OperationalReplayPressureLevel.Elevated
            : OperationalReplayPressureLevel.Nominal;
    }

    private static OperationalReplayStabilizationState ClassifyStabilizationPressureState(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayRuntimeSignals runtimeSignals,
        bool escalationVisible,
        bool recoveryProgression)
    {
        if (runtimeSignals.ProtectiveContainmentActive || reconciliationWorkbench.ReplayRisk.ProtectiveModeActive)
            return OperationalReplayStabilizationState.Contained;
        if (escalationVisible && !recoveryProgression)
            return OperationalReplayStabilizationState.Escalating;
        if (recoveryProgression || reconciliation.InvestigatingCount > 0)
            return OperationalReplayStabilizationState.Stabilizing;
        if (resilience.ReplayStormRiskIndicated)
            return OperationalReplayStabilizationState.InterventionRecommended;

        return OperationalReplayStabilizationState.Stable;
    }

    private static OperationalReplayRecoveryConfidence ClassifyRecoveryConfidence(
        OperationalResilienceSummaryDto resilience,
        OperationalDashboardSummaryDto dashboard,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto stabilization,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalReplayRuntimeSignals runtimeSignals)
    {
        if (resilience.ReplayStormRiskIndicated
            || stabilization.ReplayPressureEscalating
            || runtimeSignals.RuntimeSaturationIndicated)
            return OperationalReplayRecoveryConfidence.Fragile;

        if (stabilization.ReplayRecoveryStalled
            || dashboard.Pressure.ProtectiveModeActive
            || governanceOverview.AgingEntryCount + governanceOverview.ExpiredEntryCount >= 3)
            return OperationalReplayRecoveryConfidence.Uncertain;

        if (stabilization.ReplayRecoveryImproving
            || reconciliationWorkbench.ReplayRisk.StabilizationRecovering)
            return OperationalReplayRecoveryConfidence.Recovering;

        return OperationalReplayRecoveryConfidence.Stable;
    }

    private static OperationalReplayPressureLevel ClassifyCountSeverity(
        int count,
        int elevatedThreshold,
        int highThreshold,
        int criticalThreshold)
    {
        if (count >= criticalThreshold)
            return OperationalReplayPressureLevel.Critical;
        if (count >= highThreshold)
            return OperationalReplayPressureLevel.High;
        if (count >= elevatedThreshold)
            return OperationalReplayPressureLevel.Elevated;

        return count > 0 ? OperationalReplayPressureLevel.Elevated : OperationalReplayPressureLevel.Nominal;
    }

    private static string DescribePressureSummary(
        OperationalReplayPressureLevel level,
        int activePressure,
        bool protectiveVisible,
        bool recoveryProgression)
    {
        if (level == OperationalReplayPressureLevel.Nominal && activePressure == 0)
            return "Nominal — no significant replay pressure detected.";

        var mode = protectiveVisible ? " Protective mode is visible." : string.Empty;
        var recovery = recoveryProgression ? " Recovery progression indicated." : string.Empty;
        return $"{level} replay instability — active pressure={activePressure}.{mode}{recovery}";
    }

    private static string DescribeStabilization(
        bool recovering,
        bool stalled,
        bool escalating,
        bool protective,
        bool intervention)
    {
        if (protective)
            return "Protective containment active — replay detail may be reduced under load.";
        if (escalating && !recovering)
            return "Replay pressure is escalating — stabilization has not yet taken hold.";
        if (recovering)
            return "Replay recovery is improving while stabilization remains active.";
        if (stalled)
            return "Replay recovery appears stalled — operator review may be needed.";
        if (intervention)
            return "Operator intervention recommended to assess replay stabilization path.";

        return "Replay stabilization conditions appear stable.";
    }

    private static string DescribeRecoveryConfidence(
        OperationalReplayRecoveryConfidence confidence,
        bool recovering) =>
        confidence switch
        {
            OperationalReplayRecoveryConfidence.Stable =>
                "Stable — replay conditions appear within normal operational parameters.",
            OperationalReplayRecoveryConfidence.Recovering =>
                recovering
                    ? "Recovering — replay pressure is trending toward stabilization."
                    : "Recovering — early signals suggest replay conditions are improving.",
            OperationalReplayRecoveryConfidence.Uncertain =>
                "Uncertain — mixed signals prevent confident replay recovery assessment.",
            OperationalReplayRecoveryConfidence.Fragile =>
                "Fragile — replay recovery is easily disrupted; exercise caution before relying on summaries.",
            _ => "Recovery confidence is advisory only."
        };
}
