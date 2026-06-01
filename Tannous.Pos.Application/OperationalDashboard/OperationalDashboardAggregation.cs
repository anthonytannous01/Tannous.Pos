using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Deterministic operator dashboard composition from existing diagnostics summaries.</summary>
public static class OperationalDashboardAggregation
{
    public const int MaxRecommendations = 6;
    public const int MaxActiveConcerns = 8;

    public static OperationalDashboardHealthDto ComposeHealth(
        OperationalResilienceSummaryDto resilience,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalGovernanceRuntimeProtectionDto runtimeProtection,
        OperationalGovernanceFingerprintDto fingerprint)
    {
        var factors = new List<string>();

        if (resilience.UnresolvedConflictCount > 0)
            factors.Add($"UnresolvedConflicts:{resilience.UnresolvedConflictCount}");
        if (alerts.CriticalSignals > 0)
            factors.Add($"CriticalAlerts:{alerts.CriticalSignals}");
        if (incidents.CriticalIncidentCount > 0)
            factors.Add($"CriticalIncidents:{incidents.CriticalIncidentCount}");
        if (resilience.ReplayStormRiskIndicated)
            factors.Add("ReplayStormRisk");
        if (runtimeProtection.Failsafe.FailsafeActive)
            factors.Add("ProtectiveModeActive");
        if (fingerprint.FingerprintChanged)
            factors.Add("ConfigurationDriftDetected");

        var state = ClassifyHealth(resilience, alerts, incidents, runtimeProtection, fingerprint);
        var attention = ClassifyAttention(state, alerts, resilience);

        return new OperationalDashboardHealthDto
        {
            State = state,
            AttentionState = attention,
            Summary = DescribeHealth(state, attention),
            HealthFactors = ClampOrdered(factors, 6)
        };
    }

    public static OperationalDashboardRiskDto ComposeRisk(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents)
    {
        var primaryRisks = new List<string>();

        if (reconciliation.UnresolvedCount > 0)
            primaryRisks.Add("ReconciliationBacklog");
        if (reconciliation.ReplayMismatchCount > 0)
            primaryRisks.Add("ReplayMismatch");
        if (reconciliation.InventoryDriftRiskCount > 0)
            primaryRisks.Add("InventoryDrift");
        if (resilience.ReplayStormRiskIndicated)
            primaryRisks.Add("ReplayPressure");
        if (incidents.HighRiskIncidentCount > 0)
            primaryRisks.Add("CorrelatedIncidents");
        if (alerts.CriticalSignals > 0)
            primaryRisks.Add("CriticalAlerts");

        var level = ClassifyRisk(reconciliation, alerts, incidents, resilience);

        return new OperationalDashboardRiskDto
        {
            Level = level,
            Summary = DescribeRisk(level),
            UnresolvedConflictCount = reconciliation.UnresolvedCount,
            CriticalAlertCount = alerts.CriticalSignals,
            HighRiskIncidentCount = incidents.HighRiskIncidentCount,
            PrimaryRisks = ClampOrdered(primaryRisks, 6)
        };
    }

    public static OperationalDashboardPressureDto ComposePressure(
        OperationalResilienceSummaryDto resilience,
        OperationalGovernanceRuntimeProtectionDto runtimeProtection,
        OperationalCacheGovernanceOverviewDto governanceOverview)
    {
        var signals = new List<string>();

        if (resilience.QueryPressureIndicated)
            signals.Add("QueryPressure");
        if (resilience.ReplayStormRiskIndicated)
            signals.Add("ReplayPressure");
        if (resilience.ExportTruncationPressureIndicated)
            signals.Add("ExportPressure");
        if (resilience.AuditPersistencePressureIndicated)
            signals.Add("AuditPersistencePressure");
        if (IsRuntimeSaturation(runtimeProtection))
            signals.Add("RuntimeSaturation");
        if (runtimeProtection.Failsafe.FailsafeActive)
            signals.Add("ProtectiveMode");

        var protectiveMode = runtimeProtection.Failsafe.FailsafeActive
            || string.Equals(runtimeProtection.ExecutionState, "Failsafe", StringComparison.OrdinalIgnoreCase);

        return new OperationalDashboardPressureDto
        {
            Summary = DescribePressure(signals.Count, protectiveMode),
            QueryPressureIndicated = resilience.QueryPressureIndicated,
            ReplayStormRiskIndicated = resilience.ReplayStormRiskIndicated,
            ExportPressureIndicated = resilience.ExportTruncationPressureIndicated,
            AuditPersistencePressureIndicated = resilience.AuditPersistencePressureIndicated,
            RuntimeSaturationIndicated = IsRuntimeSaturation(runtimeProtection),
            ProtectiveModeActive = protectiveMode,
            PressureSignals = ClampOrdered(signals, 6)
        };
    }

    public static OperationalDashboardActivityDto ComposeActivity(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents)
    {
        return new OperationalDashboardActivityDto
        {
            ActiveAlertCount = alerts.TotalSignals,
            UnresolvedReconciliationCount = reconciliation.UnresolvedCount,
            InvestigatingReconciliationCount = reconciliation.InvestigatingCount,
            IncidentGroupCount = incidents.TotalIncidentGroups,
            ReplayMismatchCount = reconciliation.ReplayMismatchCount,
            InventoryDriftRiskCount = reconciliation.InventoryDriftRiskCount,
            Summary = DescribeActivity(reconciliation, alerts, incidents)
        };
    }

    public static IReadOnlyList<string> ComposeActiveConcerns(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalGovernanceRuntimeProtectionDto runtimeProtection,
        OperationalGovernanceFingerprintDto fingerprint)
    {
        var concerns = new List<string>();

        if (reconciliation.UnresolvedCount > 0)
            concerns.Add($"Reconciliation backlog: {reconciliation.UnresolvedCount} unresolved conflict(s).");
        if (alerts.CriticalSignals > 0)
            concerns.Add($"Critical alerts active: {alerts.CriticalSignals}.");
        if (incidents.CriticalIncidentCount > 0)
            concerns.Add($"Critical correlated incidents: {incidents.CriticalIncidentCount}.");
        if (reconciliation.InventoryDriftRiskCount > 0)
            concerns.Add($"Inventory drift risk conflicts: {reconciliation.InventoryDriftRiskCount}.");
        if (resilience.ReplayStormRiskIndicated)
            concerns.Add("Elevated replay pressure detected.");
        if (runtimeProtection.Failsafe.FailsafeActive)
            concerns.Add("System protective mode is active.");
        if (fingerprint.FingerprintChanged)
            concerns.Add("Operational configuration drift detected since last snapshot.");
        if (resilience.ExportTruncationPressureIndicated)
            concerns.Add("Export volume pressure may truncate forensic exports.");

        return ClampOrdered(concerns, MaxActiveConcerns);
    }

    public static IReadOnlyList<string> ComposeRecommendations(
        OperationalResilienceSummaryDto resilience,
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalCacheGovernanceOverviewDto governanceOverview,
        OperationalGovernanceRuntimeProtectionDto runtimeProtection)
    {
        var recommendations = new List<(int Priority, string Text)>();

        if (reconciliation.UnresolvedCount > 0)
        {
            recommendations.Add((1, "Review reconciliation backlog and triage unresolved conflicts."));
        }

        if (resilience.ReplayStormRiskIndicated || reconciliation.ReplayMismatchCount > 0)
        {
            recommendations.Add((2, "Investigate replay pressure and verify device sync activity."));
        }

        if (governanceOverview.AgingEntryCount > 0
            || governanceOverview.NearExpiryEntryCount > 0
            || governanceOverview.ExpiredEntryCount > 0)
        {
            recommendations.Add((3, "Refresh stale diagnostics by reviewing cache freshness indicators."));
        }

        if (reconciliation.InventoryDriftRiskCount > 0)
        {
            recommendations.Add((4, "Review inventory drift conflicts and validate stock reconciliation."));
        }

        if (resilience.ExportTruncationPressureIndicated)
        {
            recommendations.Add((5, "Reduce export pressure by narrowing forensic export scope."));
        }

        if (alerts.CriticalSignals > 0 || alerts.WarningSignals > 0)
        {
            recommendations.Add((6, "Review active alert signals and correlate with incidents."));
        }

        if (runtimeProtection.Failsafe.FailsafeActive)
        {
            recommendations.Add((7, "Review system load — protective limits are reducing advisory detail."));
        }

        return recommendations
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Text, StringComparer.Ordinal)
            .Take(MaxRecommendations)
            .Select(r => r.Text)
            .ToList();
    }

    public static string ComposeReadinessSummary(OperationalGovernanceProductionReadinessDto readiness) =>
        readiness.ReadinessState switch
        {
            "OperationallyStable" => "Operations are running within normal parameters.",
            "IntegrationReady" => "Operations are stable with minor advisory signals — suitable for integration monitoring.",
            "GovernanceSaturated" => "Operational load is elevated — review active concerns and reduce query/export pressure.",
            "DevelopmentReady" => "Operational diagnostics are warming up — monitor before production reliance.",
            _ => "Operational readiness is advisory — review health and risk sections."
        };

    public static string ComposeFingerprintStabilitySummary(OperationalGovernanceFingerprintDto fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint.FingerprintHash))
            return "Configuration fingerprint unavailable.";

        if (!fingerprint.HasPreviousFingerprint)
            return "Configuration baseline established — stability will be tracked on subsequent reads.";

        if (fingerprint.FingerprintChanged)
            return "Configuration changed since last baseline — review recent operational changes.";

        return string.IsNullOrWhiteSpace(fingerprint.FingerprintStability)
            ? "Configuration is stable relative to the previous baseline."
            : $"Configuration stability: {fingerprint.FingerprintStability}.";
    }

    private static OperationalDashboardHealthState ClassifyHealth(
        OperationalResilienceSummaryDto resilience,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalGovernanceRuntimeProtectionDto runtimeProtection,
        OperationalGovernanceFingerprintDto fingerprint)
    {
        if (alerts.CriticalSignals >= 3
            || (alerts.CriticalSignals > 0 && resilience.UnresolvedConflictCount >= 5)
            || (runtimeProtection.Failsafe.FailsafeActive && resilience.UnresolvedConflictCount > 0))
            return OperationalDashboardHealthState.Critical;

        if (resilience.UnresolvedConflictCount > 0
            || incidents.HighRiskIncidentCount > 0
            || incidents.CriticalIncidentCount > 0
            || runtimeProtection.Failsafe.FailsafeActive
            || fingerprint.FingerprintChanged
            || IsRuntimeSaturation(runtimeProtection))
            return OperationalDashboardHealthState.Degraded;

        if (alerts.WarningSignals > 0
            || alerts.CriticalSignals > 0
            || resilience.ReplayStormRiskIndicated
            || resilience.ExportTruncationPressureIndicated
            || resilience.AuditPersistencePressureIndicated
            || resilience.QueryPressureIndicated)
            return OperationalDashboardHealthState.AttentionRequired;

        return OperationalDashboardHealthState.Healthy;
    }

    private static OperationalDashboardAttentionState ClassifyAttention(
        OperationalDashboardHealthState health,
        OperationalAlertSummaryDto alerts,
        OperationalResilienceSummaryDto resilience) =>
        health switch
        {
            OperationalDashboardHealthState.Critical => OperationalDashboardAttentionState.Urgent,
            OperationalDashboardHealthState.Degraded => OperationalDashboardAttentionState.ActionNeeded,
            OperationalDashboardHealthState.AttentionRequired => alerts.CriticalSignals > 0
                || resilience.ReplayStormRiskIndicated
                    ? OperationalDashboardAttentionState.ActionNeeded
                    : OperationalDashboardAttentionState.Monitoring,
            _ => OperationalDashboardAttentionState.Normal
        };

    private static OperationalDashboardRiskLevel ClassifyRisk(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents,
        OperationalResilienceSummaryDto resilience)
    {
        if (alerts.CriticalSignals >= 3
            || incidents.CriticalIncidentCount >= 2
            || (reconciliation.UnresolvedCount >= 10 && alerts.CriticalSignals > 0))
            return OperationalDashboardRiskLevel.Critical;

        if (alerts.CriticalSignals > 0
            || incidents.HighRiskIncidentCount >= 3
            || reconciliation.UnresolvedCount >= 10
            || resilience.ReplayStormRiskIndicated)
            return OperationalDashboardRiskLevel.High;

        if (reconciliation.UnresolvedCount > 0
            || incidents.HighRiskIncidentCount > 0
            || reconciliation.InventoryDriftRiskCount > 0
            || alerts.WarningSignals >= 3)
            return OperationalDashboardRiskLevel.Elevated;

        if (alerts.TotalSignals > 0 || incidents.TotalIncidentGroups > 0)
            return OperationalDashboardRiskLevel.Moderate;

        return OperationalDashboardRiskLevel.Low;
    }

    private static bool IsRuntimeSaturation(OperationalGovernanceRuntimeProtectionDto runtimeProtection) =>
        string.Equals(runtimeProtection.TelemetrySaturationLevel, "Elevated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeProtection.TelemetrySaturationLevel, "Saturated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeProtection.BudgetPressure, "Elevated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeProtection.BudgetPressure, "Critical", StringComparison.OrdinalIgnoreCase);

    private static string DescribeHealth(
        OperationalDashboardHealthState state,
        OperationalDashboardAttentionState attention) =>
        $"{state} — {attention}";

    private static string DescribeRisk(OperationalDashboardRiskLevel level) =>
        level switch
        {
            OperationalDashboardRiskLevel.Low => "No significant operational risks detected.",
            OperationalDashboardRiskLevel.Moderate => "Minor operational risks present — monitor active signals.",
            OperationalDashboardRiskLevel.Elevated => "Elevated operational risks require review.",
            OperationalDashboardRiskLevel.High => "High operational risk — prioritize reconciliation and alerts.",
            OperationalDashboardRiskLevel.Critical => "Critical operational risk — immediate operator review recommended.",
            _ => "Operational risk is advisory only."
        };

    private static string DescribePressure(int signalCount, bool protectiveMode)
    {
        if (protectiveMode)
            return "Protective mode active — operational detail may be reduced under load.";

        return signalCount == 0
            ? "No significant operational pressure detected."
            : $"{signalCount} pressure signal(s) active.";
    }

    private static string DescribeActivity(
        ReconciliationSummaryDto reconciliation,
        OperationalAlertSummaryDto alerts,
        OperationalIncidentSummaryDto incidents)
    {
        return $"Alerts={alerts.TotalSignals}, UnresolvedConflicts={reconciliation.UnresolvedCount}, "
            + $"IncidentGroups={incidents.TotalIncidentGroups}";
    }

    private static IReadOnlyList<string> ClampOrdered(IReadOnlyList<string> values, int max) =>
        values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal)
            .Take(max)
            .ToList();
}
