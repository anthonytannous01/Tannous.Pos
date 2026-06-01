namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceDriftAnalysisDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = string.Empty;
    public string FingerprintHash { get; init; } = string.Empty;
    public string FingerprintStability { get; init; } = string.Empty;
    public string DriftDirection { get; init; } = string.Empty;
    public bool FingerprintChanged { get; init; }
    public bool HasPreviousFingerprint { get; init; }
    public string? PreviousFingerprintHash { get; init; }
    public long GovernanceFingerprintTransitions { get; init; }
    public long GovernanceDriftEscalations { get; init; }
    public long GovernanceStableFingerprintHits { get; init; }
    public IReadOnlyList<string> DriftSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExplainabilityCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
