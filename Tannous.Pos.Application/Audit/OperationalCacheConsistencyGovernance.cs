namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Step 11 consistency recovery & drift containment (process-local; advisory only).
/// GOVERNANCE: not persisted; not authoritative; no auto-remediation.
/// </summary>
public static class OperationalCacheConsistencyGovernance
{
    public const int MaxExplainabilityItems = 8;
    public const int MaxReasonCodeLength = 48;
    public const int MaxRecommendations = 8;
    public const int HighChurnInvalidationThreshold = 10;
    public const int StabilizationChurnReboundThreshold = 5;
    public const double LowHitRatioThreshold = 0.35;
    public const double ElevatedBypassRatioThreshold = 0.25;
    public const int RecoveryWindowExtensionInvalidationThreshold = 8;

    public static string GetAssumption() =>
        "Consistency recovery and containment projections are heuristic operator guidance only; not durable across restarts.";

    public static string GetContainmentAssumption() =>
        "Containment states describe in-process cache coherence visibility; operators must re-query after material operational changes.";
}
