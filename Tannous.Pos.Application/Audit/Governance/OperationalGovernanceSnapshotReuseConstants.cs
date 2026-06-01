namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Bounded governance snapshot reuse settings (process-local; no timers/workers).
/// GOVERNANCE: not business IMemoryCache semantics.
/// </summary>
public static class OperationalGovernanceSnapshotReuseConstants
{
    public const int TtlSeconds = 7;
    public const int AgingThresholdSeconds = 5;
    public const int MaxSnapshotKeys = 3;
}
