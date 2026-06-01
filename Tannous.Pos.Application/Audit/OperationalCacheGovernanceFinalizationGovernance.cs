namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Step 9 governance finalization: auditability, explainability, survivability (process-local; advisory only).
/// GOVERNANCE: projections are computed at read time; not persisted; not authoritative.
/// GOVERNANCE: drift detection does not auto-remediate; eventual freshness still applies.
/// </summary>
public static class OperationalCacheGovernanceFinalizationGovernance
{
    public const int MaxExplainabilityItems = 8;
    public const int MaxReasonCodeLength = 48;

    public static string GetAssumption() =>
        "Governance audit, drift, consistency, and survivability outputs are heuristic advisory projections for operators only.";

    public static string GetExplainabilityAssumption() =>
        "Reason codes and trigger signals describe classification inputs only; no payload or implementation detail exposure.";
}
