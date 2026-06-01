namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Advisory stability of governance projection fingerprints within a process window.</summary>
public enum OperationalGovernanceFingerprintStability
{
    Stable,
    Transitional,
    Diverging,
    Fragmented
}

/// <summary>Advisory drift direction between consecutive governance fingerprints.</summary>
public enum OperationalGovernanceDriftDirection
{
    Neutral,
    Improving,
    Degrading,
    Oscillating
}

/// <summary>Advisory replay consistency of governance snapshot builds.</summary>
public enum OperationalGovernanceReplayConsistencyLevel
{
    High,
    Moderate,
    Low,
    Indeterminate
}
