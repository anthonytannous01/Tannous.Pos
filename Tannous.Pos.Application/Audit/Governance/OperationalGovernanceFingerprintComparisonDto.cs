namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceFingerprintComparisonDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string CurrentFingerprintHash { get; init; } = string.Empty;
    public string? PreviousFingerprintHash { get; init; }
    public bool FingerprintChanged { get; init; }
    public string DriftDirection { get; init; } = string.Empty;
    public string FingerprintStability { get; init; } = string.Empty;
    public int DivergentSegmentCount { get; init; }
    public IReadOnlyList<string> DivergentSegments { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ComparisonSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
