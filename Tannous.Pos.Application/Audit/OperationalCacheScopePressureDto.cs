namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheScopePressureDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalCachePressureSeverity Severity { get; init; }
    public OperationalCacheCardinalityClassification Cardinality { get; init; }
    public double ScopedEntryRatio { get; init; }
    public long RepeatedColdMisses { get; init; }
    public long InvalidationChurn { get; init; }
    public string RecommendedOperatorAction { get; init; } = string.Empty;
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
