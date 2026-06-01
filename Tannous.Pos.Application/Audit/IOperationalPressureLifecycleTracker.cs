namespace Tannous.Pos.Application.Audit;

/// <summary>In-process pressure lifecycle epoch tracking (governance only; not persisted).</summary>
public interface IOperationalPressureLifecycleTracker
{
    OperationalPressureLifecycleSnapshot GetSnapshot();

    void NotePressureElevated(bool queryClamped, bool pageClamped, bool exportTruncated);

    void NoteGovernanceReset();

    void NoteRecoveryTransition();

    void NoteConvergenceRecovery();
}

public sealed class OperationalPressureLifecycleSnapshot
{
    public long ActiveEpoch { get; init; }
    public string LifecycleState { get; init; } = string.Empty;
    public long LifecycleTransitions { get; init; }
    public long StabilizationCycles { get; init; }
    public bool StickyPressureDetected { get; init; }
    public DateTime? LastResetUtc { get; init; }
    public DateTime? LastRecoveryUtc { get; init; }
}
