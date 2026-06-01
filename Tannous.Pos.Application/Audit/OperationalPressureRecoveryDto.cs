namespace Tannous.Pos.Application.Audit;

public sealed class OperationalPressureRecoveryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string RecoveryClassification { get; init; } = string.Empty;
    public string LifecycleState { get; init; } = string.Empty;
    public long PressureRecoveryCycles { get; init; }
    public long StickyPressureRecoveries { get; init; }
    public long AdaptiveTtlRecoveries { get; init; }
    public bool PressureFlagsCleared { get; init; }
    public OperationalPressureStabilizationWindowDto StabilizationWindow { get; init; } = new();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
