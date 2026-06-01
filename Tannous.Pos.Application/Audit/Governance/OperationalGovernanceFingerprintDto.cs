namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceFingerprintDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = string.Empty;
    public string Profile { get; init; } = string.Empty;
    public string FingerprintHash { get; init; } = string.Empty;
    public string ExplainabilityHash { get; init; } = string.Empty;
    public OperationalGovernanceProjectionSignatureDto Signature { get; init; } = new();
    public string FingerprintStability { get; init; } = string.Empty;
    public bool HasPreviousFingerprint { get; init; }
    public string? PreviousFingerprintHash { get; init; }
    public bool FingerprintChanged { get; init; }
    public IReadOnlyList<string> ExplainabilityCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
