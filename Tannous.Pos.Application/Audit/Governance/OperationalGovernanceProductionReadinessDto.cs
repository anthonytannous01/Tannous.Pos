namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceProductionReadinessDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ReadinessState { get; init; } = string.Empty;
    public double HitRatioStabilityScore { get; init; }
    public long BypassPressureEvents { get; init; }
    public long InvalidationChurn { get; init; }
    public long RuntimeFailsafeActivations { get; init; }
    public double SnapshotReuseRatio { get; init; }
    public long FingerprintTransitions { get; init; }
    public IReadOnlyList<string> ReadinessSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
