namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceProjectionConsistencyDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ConsistencyLevel { get; init; } = OperationalGovernanceSnapshotConsistencyLevel.Strong.ToString();
    public string SnapshotState { get; init; } = OperationalGovernanceSnapshotState.Fresh.ToString();
    public string ReuseLevel { get; init; } = OperationalGovernanceProjectionReuseLevel.None.ToString();
    public double SnapshotAgeSeconds { get; init; }
    public int ProjectionCount { get; init; }
    public int ExplainabilitySignalCount { get; init; }
    public long SnapshotConsistencyTransitions { get; init; }
    public IReadOnlyList<string> ConsistencySignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
