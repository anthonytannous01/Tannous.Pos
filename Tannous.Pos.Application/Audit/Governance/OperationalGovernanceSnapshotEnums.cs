namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Governance snapshot freshness (projection-only; not business data freshness).</summary>
public enum OperationalGovernanceSnapshotState
{
    Fresh,
    Reused,
    Aging,
    Expired
}

public enum OperationalGovernanceProjectionReuseLevel
{
    None,
    Partial,
    Significant,
    Dominant
}

/// <summary>Advisory consistency of reused governance projections within a snapshot.</summary>
public enum OperationalGovernanceSnapshotConsistencyLevel
{
    Strong,
    Stable,
    Transitional,
    Fragmented
}
