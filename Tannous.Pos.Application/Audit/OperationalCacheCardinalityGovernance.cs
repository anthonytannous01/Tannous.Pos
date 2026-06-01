namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Cardinality governance for operational diagnostics cache (in-process only).
/// GOVERNANCE: deterministic heuristics; not persisted; not authoritative.
/// GOVERNANCE / NON-GOAL: no machine memory inspection; no distributed coordination.
/// </summary>
public static class OperationalCacheCardinalityGovernance
{
    public const int ElevatedScopedKeyThreshold = 4;
    public const int HighScopedKeyThreshold = 12;
    public const double ElevatedActiveEntryRatio = 0.5;
    public const double HighActiveEntryRatio = 0.75;
    public const double SaturatedActiveEntryRatio = 0.95;

    public static string GetAssumption() =>
        "Cardinality classifications use active entry and scoped-key counts only; not OS memory metrics.";
}
