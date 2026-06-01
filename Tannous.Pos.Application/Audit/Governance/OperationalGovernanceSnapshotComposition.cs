namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Process-local reusable governance projection bundle (not persisted; not business cache payloads).
/// </summary>
public sealed class OperationalGovernanceSnapshotComposition
{
    public required string SnapshotKey { get; init; }

    public required OperationalGovernanceProfile Profile { get; init; }

    public required DateTime CreatedUtc { get; init; }

    public required OperationalGovernanceCompositionContext Context { get; init; }

    public required OperationalGovernanceRuntimeProtectionDto RuntimeProtection { get; init; }

    public required OperationalGovernanceTelemetrySaturationDto TelemetrySaturation { get; init; }

    public required OperationalGovernanceExecutionDiagnosticsDto ExecutionDiagnostics { get; init; }

    public required OperationalCacheGovernanceConsistencyDto GovernanceConsistency { get; init; }

    public required IReadOnlyList<string> ExplainabilityCodes { get; init; }

    public required string FingerprintHash { get; init; }

    public required string NormalizedSignature { get; init; }

    public required IReadOnlyList<string> SignatureSegments { get; init; }

    public int BuildElapsedMilliseconds { get; init; }

    public int ProjectionCount =>
        9;
}
