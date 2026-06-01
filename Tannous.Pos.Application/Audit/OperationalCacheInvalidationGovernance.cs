namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Step 10 invalidation governance: operator audit projections (process-local; advisory only).
/// GOVERNANCE: not persisted; not authoritative; no auto-remediation.
/// </summary>
public static class OperationalCacheInvalidationGovernance
{
    public const int MaxReasonCodes = 8;
    public const int MaxReasonCodeLength = 48;
    public const int MaxRecommendations = 8;
    public const int HighInvalidationChurnThreshold = 15;
    public const int CriticalInvalidationChurnThreshold = 30;
    public const double ElevatedScopeChurnRatio = 0.35;
    public const double HighScopeChurnRatio = 0.55;

    public static string GetAssumption() =>
        "Invalidation audit projections describe in-process cache metadata and telemetry only; not durable across restarts.";

    public static string GetRecoveryAssumption() =>
        "Freshness recovery states are derived from invalidation churn and stale-risk metadata; operators must re-query diagnostics after material changes.";
}
