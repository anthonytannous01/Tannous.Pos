namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceReplayConsistencyDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = string.Empty;
    public string FingerprintHash { get; init; } = string.Empty;
    public string ReplayConsistencyLevel { get; init; } = string.Empty;
    public bool SnapshotWasReused { get; init; }
    public bool FingerprintStableAcrossReuse { get; init; }
    public long ReplayConsistencyChecks { get; init; }
    public long ProjectionFragmentationSignals { get; init; }
    public IReadOnlyList<string> ConsistencySignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExplainabilityCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
