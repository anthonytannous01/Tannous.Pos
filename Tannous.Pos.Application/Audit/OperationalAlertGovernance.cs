namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Alert signal governance (heuristic, in-process, query-time only).
/// GOVERNANCE / NON-GOAL: alerts are NOT persisted.
/// GOVERNANCE / NON-GOAL: alerts are NOT delivered externally (no email/SMS/push/webhooks).
/// GOVERNANCE / NON-GOAL: alerts are NOT guaranteed and may be absent after process restart.
/// GOVERNANCE / NON-GOAL: no paging/on-call integration exists.
/// GOVERNANCE / NON-GOAL: no automatic remediation, retries, throttling, or distributed coordination.
/// Operators remain responsible for manual action via existing diagnostics endpoints.
/// </summary>
public static class OperationalAlertGovernance
{
    public static string GetEscalationRecommendation(string alertType, string severity) =>
        severity switch
        {
            OperationalAlertSeverity.Critical when alertType == OperationalAlertTypes.ReplayStormRisk =>
                "Treat as elevated replay pressure; inspect durable receipts and reconciliation backlog before bulk client retries.",
            OperationalAlertSeverity.Critical when alertType == OperationalAlertTypes.AuditPersistencePressure =>
                "Audit persistence pressure is critical; verify database health and review recent operational audit failures.",
            OperationalAlertSeverity.Critical when alertType == OperationalAlertTypes.CascadingOperationalPressure =>
                "Multiple subsystems show strain; prioritize forensic export and incident correlation summaries.",
            OperationalAlertSeverity.Warning when alertType == OperationalAlertTypes.ReconciliationBacklog =>
                "Unresolved reconciliation items are accumulating; use reconciliation workflow endpoints.",
            OperationalAlertSeverity.Warning when alertType == OperationalAlertTypes.InventoryDriftEscalation =>
                "Inventory drift signals detected; verify finalize/void paths and stock movements for affected orders.",
            OperationalAlertSeverity.Warning when alertType == OperationalAlertTypes.ExportTruncationPressure =>
                "Forensic exports are truncated; narrow scope or use multiple exports; not a complete archive.",
            _ when severity == OperationalAlertSeverity.Info =>
                "Informational signal only; no escalation required unless trend worsens.",
            _ => "Review correlated diagnostics; manual operator judgment required."
        };

    public static string GetSuggestedOperatorAction(string alertType) =>
        alertType switch
        {
            OperationalAlertTypes.ReplayStormRisk =>
                "Open replay-risk and incident summaries; inspect sync receipts for hot deviceId/operationId keys.",
            OperationalAlertTypes.AuditPersistencePressure =>
                "Check operational audit diagnostics; confirm audit rows still append for money/inventory paths.",
            OperationalAlertTypes.InventoryDriftEscalation =>
                "Filter reconciliation unresolved list for inventory drift; reconcile affected orders.",
            OperationalAlertTypes.CascadingOperationalPressure =>
                "Open cascading-degradation and resilience summaries; correlate subsystems before changes.",
            OperationalAlertTypes.ReconciliationBacklog =>
                "Triage unresolved sync conflicts; acknowledge/investigate/resolve per workflow.",
            OperationalAlertTypes.ConflictEscalation =>
                "Review conflict diagnostics and forensic export for correlated operationId.",
            OperationalAlertTypes.ExportTruncationPressure =>
                "Re-run scoped forensic export; expect caps on audit/conflict/replay sections.",
            OperationalAlertTypes.LifecycleConflictSpike =>
                "Review lifecycle conflicts on finalize/void; confirm clients refresh order state.",
            _ => "Use internal operational-audit diagnostics endpoints for context."
        };

    public static string GetNonGoalsStatement() =>
        "Alert signals are heuristic, in-process, query-time visibility only; not persisted; not delivered; not guaranteed; no on-call integration.";
}
