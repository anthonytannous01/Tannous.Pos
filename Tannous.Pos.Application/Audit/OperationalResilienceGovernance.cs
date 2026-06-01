namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Operational resilience and degraded-mode governance (informational diagnostics only).
/// GOVERNANCE NON-GOALS: no distributed circuit breaker mesh, no external queueing, no auto failover,
/// no autoscaling orchestration, no Kubernetes/operator logic. Audit remains best-effort under strain.
/// </summary>
public static class OperationalResilienceGovernance
{
    public static string GetSurvivabilityAssumption(string degradedMode) =>
        degradedMode switch
        {
            OperationalDegradedModeTypes.Normal =>
                "Standard internal diagnostics; bounded queries and forensic caps apply.",
            OperationalDegradedModeTypes.ElevatedQueryPressure =>
                "Large-range or max-pagination diagnostics detected; prefer narrower filters.",
            OperationalDegradedModeTypes.ReconciliationPressure =>
                "Unresolved conflict backlog elevated; manual reconciliation review recommended.",
            OperationalDegradedModeTypes.ExportPressure =>
                "Forensic export nearing aggregation caps; expect truncation flags in snapshots.",
            OperationalDegradedModeTypes.AuditPersistencePressure =>
                "Operational audit persistence failures observed; business paths remain non-blocking.",
            OperationalDegradedModeTypes.ReplayStormRisk =>
                "High durable replay receipt volume; verify device operationId churn and replay short-circuits.",
            _ => "Use internal resilience summary for operator triage."
        };

    public static string GetQueryPressureExpectation() =>
        "Internal Admin queries should use bounded date ranges (max 90 days when filtered) and pagination <= 200.";

    public static string GetForensicExportPressureExpectation() =>
        "Forensic exports are capped (500 audit / 100 conflicts / 50 receipts); truncation is visibility-only.";

    public static string GetReconciliationScalingAssumption() =>
        "Reconciliation is operator-driven; backlog severity is classified, not auto-healed.";

    public static string GetAuditSurvivabilityUnderStrain() =>
        "Audit persistence is best-effort in an isolated scope; failures never block money/sync/replay paths.";
}
