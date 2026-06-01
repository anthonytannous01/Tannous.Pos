namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Step 12 pressure lifecycle governance (process-local; advisory only).
/// GOVERNANCE: non-authoritative; no cross-instance guarantees; no auto-healing; no replay correctness guarantees.
/// </summary>
public static class OperationalPressureGovernance
{
    public const int MaxExplainabilityItems = 8;
    public const int MaxReasonCodeLength = 48;
    public const int MaxRecommendations = 8;
    public const int StickyPressureEpochThreshold = 1;
    public const int ConvergenceStableScoreThreshold = 70;

    public static string GetAssumption() =>
        "Pressure lifecycle and convergence projections are heuristic operator guidance only; not durable across process restarts.";

    public static string GetResetAssumption() =>
        "Governance pressure reset clears in-process flags and advisory counters only; never mutates replay, reconciliation, or domain data.";

    public static string GetNonGoalStatement() =>
        "No automatic remediation, distributed coordination, or authoritative recovery guarantees.";
}
