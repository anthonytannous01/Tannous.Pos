namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Result of acquiring a governance snapshot (process-local reuse metadata).</summary>
public sealed class OperationalGovernanceSnapshotAccess
{
    public required OperationalGovernanceSnapshotComposition Composition { get; init; }

    public required bool WasReused { get; init; }

    public required double AgeSeconds { get; init; }

    public required bool IsExpired { get; init; }

    public OperationalGovernanceSnapshotFreshnessDto Freshness =>
        OperationalGovernanceSnapshotFreshnessClassifier.Build(AgeSeconds, WasReused, IsExpired);
}
