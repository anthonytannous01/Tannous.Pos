namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheInvalidationConsistencyDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public bool IsConsistent { get; init; }
    public string InvalidationDriftClassification { get; init; } = string.Empty;
    public IReadOnlyList<string> InconsistencySignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
