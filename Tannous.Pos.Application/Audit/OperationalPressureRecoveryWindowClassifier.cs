namespace Tannous.Pos.Application.Audit;

public static class OperationalPressureRecoveryWindowClassifier
{
    public static OperationalPressureStabilizationWindowDto Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalPressureLifecycleSnapshot lifecycle,
        bool pressureFlagsActive)
    {
        var churnRebound = pressureFlagsActive
                           && telemetry.RecoveryWindowExtensions > 0
                           && lifecycle.StabilizationCycles > 0;

        var stabilizationActive = !pressureFlagsActive
                                  && (telemetry.PressureRecoveryCycles > 0 || lifecycle.StabilizationCycles > 0);

        string classification;
        if (stabilizationActive && telemetry.StabilizationWindowResets > 0)
            classification = "Stabilized";
        else if (churnRebound)
            classification = "ChurnRebound";
        else if (telemetry.RecoveryWindowExtensions > 0)
            classification = "Extended";
        else if (pressureFlagsActive)
            classification = "ActivePressure";
        else
            classification = "Nominal";

        return new OperationalPressureStabilizationWindowDto
        {
            WindowClassification = classification,
            StabilizationActive = stabilizationActive,
            ChurnReboundDetected = churnRebound,
            RecoveryWindowExtensions = telemetry.RecoveryWindowExtensions,
            StabilizationWindowResets = telemetry.StabilizationWindowResets,
            PressureRecoveryCycles = telemetry.PressureRecoveryCycles,
            StabilizationSignals = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                stabilizationActive ? "RecoveryWindowActive" : string.Empty,
                churnRebound ? "ChurnReboundDetected" : string.Empty,
                telemetry.AdaptiveTtlRecoveries > 0 ? "AdaptiveTtlRecovered" : string.Empty,
                telemetry.PressureConvergenceRecoveries > 0 ? "PressureDecayObserved" : string.Empty
            })
        };
    }
}
