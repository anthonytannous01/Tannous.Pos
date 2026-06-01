namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceSnapshotMetadataDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = OperationalGovernanceSnapshotKeys.Standard;
    public string Profile { get; init; } = OperationalGovernanceProfile.Standard.ToString();
    public DateTime SnapshotCreatedUtc { get; init; }
    public double SnapshotAgeSeconds { get; init; }
    public string SnapshotState { get; init; } = OperationalGovernanceSnapshotState.Fresh.ToString();
    public string ReuseLevel { get; init; } = OperationalGovernanceProjectionReuseLevel.None.ToString();
    public int ProjectionCount { get; init; }
    public int ExplainabilitySignalCount { get; init; }
    public int TtlSeconds { get; init; } = OperationalGovernanceSnapshotReuseConstants.TtlSeconds;
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
