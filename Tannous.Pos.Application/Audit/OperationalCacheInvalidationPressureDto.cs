namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheInvalidationPressureDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string InvalidationSeverity { get; init; } = string.Empty;
    public long TotalInvalidations { get; init; }
    public long ScopedInvalidations { get; init; }
    public long CrossCategoryInvalidations { get; init; }
    public long InvalidationPressureEscalations { get; init; }
    public double ScopeChurnRatio { get; init; }
    public double InvalidationChurnRatio { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public IReadOnlyDictionary<string, int> InvalidationsByCategoryEstimate { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
