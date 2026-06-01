namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Incident correlation governance (heuristic, in-process, dynamic).
/// NON-GOALS: no PagerDuty; no alerting providers; no distributed tracing; no OpenTelemetry exporters; no automatic remediation.
/// </summary>
public static class OperationalIncidentGovernance
{
    public static string GetCausalityAssumption(string incidentType) =>
        incidentType switch
        {
            OperationalIncidentTypes.ReplayIncident =>
                "Replay receipt or audit replay-mismatch signals may precede reconciliation conflicts on the same operationId.",
            OperationalIncidentTypes.ReconciliationIncident =>
                "Unresolved sync conflicts are grouped by device/operation; manual reconciliation workflow applies.",
            OperationalIncidentTypes.SettlementInconsistencyIncident =>
                "Settlement audit anomalies may co-occur with concurrency conflicts on the same order.",
            OperationalIncidentTypes.InventoryDriftIncident =>
                "Negative stock or inventory drift conflicts suggest finalize/stock divergence on related entities.",
            OperationalIncidentTypes.ResiliencePressureIncident =>
                "Degraded-mode and audit persistence pressure are informational; business paths remain non-blocking.",
            OperationalIncidentTypes.ForensicSurvivabilityIncident =>
                "Export truncation indicates snapshot caps; use correlation summary for triage only.",
            OperationalIncidentTypes.CascadingDegradationIncident =>
                "Multiple subsystem signals on one correlation key suggest cascading operational strain.",
            _ => "Correlation is heuristic; verify with forensic export and reconciliation endpoints."
        };

    public static string GetEscalationGuidance(string severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical =>
                "Manual operator review required; inspect reconciliation, replay receipts, and order settlement path.",
            OperationalIncidentSeverity.High =>
                "Prioritize forensic export and unresolved conflict workflow for the correlated key.",
            OperationalIncidentSeverity.Moderate =>
                "Monitor degraded-mode summary; narrow diagnostics queries before re-export.",
            _ => "Informational grouping; no automated action."
        };

    public static string GetSubsystemGroupingRule() =>
        "Group by operationId when present, else deviceId, else orderId, else entityId; subsystem tags derive from incident type.";
}
