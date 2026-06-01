using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>In-process pressure lifecycle epoch tracking (governance only; not persisted).</summary>
public sealed class OperationalPressureLifecycleTracker : IOperationalPressureLifecycleTracker
{
    private readonly IOperationalResiliencePressureState _pressureState;
    private long _activeEpoch;
    private long _lifecycleTransitions;
    private long _stabilizationCycles;
    private volatile bool _stickyPressureDetected;
    private DateTime? _lastResetUtc;
    private DateTime? _lastRecoveryUtc;

    public OperationalPressureLifecycleTracker(IOperationalResiliencePressureState pressureState)
    {
        _pressureState = pressureState;
    }

    public OperationalPressureLifecycleSnapshot GetSnapshot()
    {
        var snapshot = new OperationalPressureLifecycleSnapshot
        {
            ActiveEpoch = _activeEpoch,
            LifecycleTransitions = _lifecycleTransitions,
            StabilizationCycles = _stabilizationCycles,
            StickyPressureDetected = _stickyPressureDetected,
            LastResetUtc = _lastResetUtc,
            LastRecoveryUtc = _lastRecoveryUtc
        };

        var lifecycle = OperationalPressureStabilizationBuilder.ClassifyLifecycle(_pressureState, snapshot);

        return new OperationalPressureLifecycleSnapshot
        {
            ActiveEpoch = _activeEpoch,
            LifecycleState = lifecycle.ToString(),
            LifecycleTransitions = _lifecycleTransitions,
            StabilizationCycles = _stabilizationCycles,
            StickyPressureDetected = _stickyPressureDetected,
            LastResetUtc = _lastResetUtc,
            LastRecoveryUtc = _lastRecoveryUtc
        };
    }

    public void NotePressureElevated(bool queryClamped, bool pageClamped, bool exportTruncated)
    {
        if (!queryClamped && !pageClamped && !exportTruncated)
            return;

        if (_lastRecoveryUtc.HasValue)
            Interlocked.Increment(ref _lifecycleTransitions);
    }

    public void NoteGovernanceReset()
    {
        if (AnyPressureActive())
            _stickyPressureDetected = true;

        Interlocked.Increment(ref _activeEpoch);
        Interlocked.Increment(ref _stabilizationCycles);
        _lastResetUtc = DateTime.UtcNow;
    }

    public void NoteRecoveryTransition()
    {
        _lastRecoveryUtc = DateTime.UtcNow;
        if (!AnyPressureActive())
            _stickyPressureDetected = false;
    }

    public void NoteConvergenceRecovery() =>
        NoteRecoveryTransition();

    private bool AnyPressureActive() =>
        _pressureState.QueryDateRangeClamped
        || _pressureState.QueryPageSizeClamped
        || _pressureState.ForensicExportTruncated;
}
