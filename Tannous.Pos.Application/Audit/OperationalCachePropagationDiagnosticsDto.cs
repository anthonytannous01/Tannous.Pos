namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCachePropagationDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string PropagationSeverity { get; init; } = string.Empty;
    public long PropagationDetections { get; init; }
    public long CrossCategoryInvalidations { get; init; }
    public long InvalidationDriftCount { get; init; }
    public IReadOnlyDictionary<string, int> CategoryExposureCounts { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyList<string> PropagationSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
