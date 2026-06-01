namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Operational diagnostics cache governance (in-process summaries/projections only).
/// GOVERNANCE: cache is in-process only; not durable; not coherent across API instances.
/// GOVERNANCE: cache is not a source of truth; diagnostics-only read models.
/// GOVERNANCE: stale reads are expected within TTL windows.
/// GOVERNANCE: targeted best-effort invalidation may follow reconciliation workflow and conflict recording; not guaranteed across instances.
/// GOVERNANCE: cache must NEVER contain payload bodies, raw forensic exports, stack traces,
/// operational audit raw metadata JSON, or EF tracked entities.
/// GOVERNANCE: audit/drift/consistency/survivability projections are advisory; explainability is heuristic only.
/// GOVERNANCE: cardinality, pressure, and degradation classifications are advisory heuristics only.
/// GOVERNANCE: no OS memory inspection (GC/working set); entry/telemetry counts only.
/// GOVERNANCE: adaptive TTL is bounded, deterministic, and never exceeds configured category TTL.
/// GOVERNANCE: warm candidates and readiness states are advisory visibility only; no background warming.
/// GOVERNANCE / NON-GOAL: no Redis; no distributed cache; no cross-node invalidation; no invalidation workers.
/// GOVERNANCE / NON-GOAL: no IHostedService/BackgroundService warming, timers, or predictive preload daemons.
/// GOVERNANCE / NON-GOAL: no event streaming; no cache replication; no write-through cache.
/// GOVERNANCE / NON-GOAL: no transactional guarantees; no replay or money-path semantic changes.
/// </summary>
public static class OperationalDiagnosticsCacheGovernance
{
    public static string GetStaleReadExpectation() =>
        "Operators may see diagnostics up to the category TTL old; bypass under export/query pressure for fresher reads.";

    public static string GetMultiInstanceAssumption() =>
        "Each API process maintains its own cache; do not assume cross-instance coherence.";

    public static string GetAllowedContentAssumption() =>
        "Only summary DTOs and numeric projections may be cached; never full forensic exports or raw audit/conflict payloads.";
}
