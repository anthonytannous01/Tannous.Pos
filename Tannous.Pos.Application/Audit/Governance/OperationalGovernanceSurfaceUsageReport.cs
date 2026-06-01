namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceSurfaceUsageReport
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalGovernanceCeilingMeasurement.OperationalGovernanceCeilingSnapshot Snapshot { get; init; } = default!;
    public bool IsWithinBudget { get; init; }
    public bool IsFreezeCompliant { get; init; }
    public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DeadSurfaceFindings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FreezeRationale { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ApprovedExtensionPolicy { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OwnershipBoundaries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}

public sealed class OperationalGovernanceDeadSurfaceDetectionResult
{
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
}
