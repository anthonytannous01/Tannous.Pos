namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Adaptive TTL and predictive warming visibility governance.
/// GOVERNANCE: advisory-only warm candidates; no background warming, timers, or prefetch daemons.
/// GOVERNANCE: process-local cache; eventual freshness still applies.
/// GOVERNANCE / NON-GOAL: Redis, distributed invalidation, guaranteed freshness, external cache infrastructure.
/// </summary>
public static class OperationalCacheAdaptiveGovernance
{
    public static string GetPredictiveWarmingAssumption() =>
        "Warm candidates and readiness states are visibility-only; the system does not perform background cache warming.";

    public static string GetAdaptiveTtlAssumption() =>
        "Adaptive TTL shrinks under deterministic pressure signals only; never exceeds configured category TTL; absolute expiration only.";

    public static string GetStabilityAssumption() =>
        "Stability score is a lightweight in-process heuristic from telemetry; not persisted and not authoritative.";
}
