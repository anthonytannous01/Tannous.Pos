namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceSnapshotFreshnessDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string FreshnessState { get; init; } = OperationalGovernanceSnapshotState.Fresh.ToString();
    public double SnapshotAgeSeconds { get; init; }
    public int TtlSeconds { get; init; } = OperationalGovernanceSnapshotReuseConstants.TtlSeconds;
    public bool WasReused { get; init; }
    public IReadOnlyList<string> FreshnessSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
