namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceProjectionReuseDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SnapshotKey { get; init; } = OperationalGovernanceSnapshotKeys.Standard;
    public string ReuseLevel { get; init; } = OperationalGovernanceProjectionReuseLevel.None.ToString();
    public string SnapshotState { get; init; } = OperationalGovernanceSnapshotState.Fresh.ToString();
    public long GovernanceSnapshotBuilds { get; init; }
    public long GovernanceSnapshotReuses { get; init; }
    public long ProjectionReuseHits { get; init; }
    public long ProjectionReuseMisses { get; init; }
    public double ReuseHitRatio { get; init; }
    public IReadOnlyList<string> ReuseSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
