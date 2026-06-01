namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheGovernanceConsistencyDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public bool IsConsistent { get; init; }
    public IReadOnlyList<string> ConsistencyNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InconsistencySignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
