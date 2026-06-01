namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceProjectionSignatureDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = string.Empty;
    public string Profile { get; init; } = string.Empty;
    public string NormalizedSignature { get; init; } = string.Empty;
    public IReadOnlyList<string> SignatureSegments { get; init; } = Array.Empty<string>();
    public int SegmentCount { get; init; }
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
