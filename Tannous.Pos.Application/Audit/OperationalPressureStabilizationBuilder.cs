namespace Tannous.Pos.Application.Audit;

public static class OperationalPressureStabilizationBuilder
{
    public static OperationalPressureLifecycleState ClassifyLifecycle(
        IOperationalResiliencePressureState pressureState,
        OperationalPressureLifecycleSnapshot lifecycle)
    {
        if (lifecycle.StickyPressureDetected && AnyPressureActive(pressureState))
            return OperationalPressureLifecycleState.Sticky;

        if (AnyPressureActive(pressureState))
            return OperationalPressureLifecycleState.Elevated;

        if (lifecycle.StabilizationCycles > 0 && lifecycle.LastRecoveryUtc.HasValue)
            return OperationalPressureLifecycleState.Stabilizing;

        if (!AnyPressureActive(pressureState) && lifecycle.LastRecoveryUtc.HasValue)
            return OperationalPressureLifecycleState.Recovered;

        return OperationalPressureLifecycleState.Nominal;
    }

    public static OperationalPressureRecoveryClassification ClassifyRecovery(
        IOperationalResiliencePressureState pressureState,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalPressureLifecycleSnapshot lifecycle)
    {
        if (!AnyPressureActive(pressureState)
            && telemetry.PressureRecoveryCycles > 0
            && telemetry.StabilizationWindowResets > 0)
            return OperationalPressureRecoveryClassification.Converged;

        if (!AnyPressureActive(pressureState) && telemetry.PressureRecoveryCycles > 0)
            return OperationalPressureRecoveryClassification.Stabilized;

        if (AnyPressureActive(pressureState) && telemetry.PressureRecoveryCycles > 0)
            return OperationalPressureRecoveryClassification.InProgress;

        if (!AnyPressureActive(pressureState))
            return OperationalPressureRecoveryClassification.NotRequired;

        return OperationalPressureRecoveryClassification.Uncertain;
    }

    private static bool AnyPressureActive(IOperationalResiliencePressureState state) =>
        state.QueryDateRangeClamped || state.QueryPageSizeClamped || state.ForensicExportTruncated;
}
