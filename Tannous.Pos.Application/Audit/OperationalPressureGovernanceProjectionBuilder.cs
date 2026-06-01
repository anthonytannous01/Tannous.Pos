namespace Tannous.Pos.Application.Audit;

public static class OperationalPressureGovernanceProjectionBuilder
{
    public static OperationalPressureLifecycleDto BuildLifecycle(
        IOperationalResiliencePressureState pressureState,
        OperationalPressureLifecycleSnapshot lifecycle)
    {
        var state = OperationalPressureStabilizationBuilder.ClassifyLifecycle(pressureState, lifecycle);

        return new OperationalPressureLifecycleDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            LifecycleState = state.ToString(),
            ActiveEpoch = lifecycle.ActiveEpoch,
            LifecycleTransitions = lifecycle.LifecycleTransitions,
            StabilizationCycles = lifecycle.StabilizationCycles,
            QueryDateRangeClamped = pressureState.QueryDateRangeClamped,
            QueryPageSizeClamped = pressureState.QueryPageSizeClamped,
            ForensicExportTruncated = pressureState.ForensicExportTruncated,
            StickyPressureDetected = lifecycle.StickyPressureDetected,
            LastResetUtc = lifecycle.LastResetUtc,
            LastRecoveryUtc = lifecycle.LastRecoveryUtc,
            ReasonCodes = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                $"Lifecycle{state}",
                lifecycle.StickyPressureDetected ? "StickyPressureDetected" : string.Empty,
                pressureState.QueryDateRangeClamped ? "QueryPressure" : string.Empty,
                pressureState.ForensicExportTruncated ? "ExportPressure" : string.Empty
            }),
            GovernanceNotes = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                OperationalPressureGovernance.GetAssumption(),
                OperationalPressureGovernance.GetNonGoalStatement()
            })
        };
    }

    public static OperationalPressureRecoveryDto BuildRecovery(
        IOperationalResiliencePressureState pressureState,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalPressureLifecycleSnapshot lifecycle)
    {
        var recovery = OperationalPressureStabilizationBuilder.ClassifyRecovery(pressureState, telemetry, lifecycle);
        var lifecycleState = OperationalPressureStabilizationBuilder.ClassifyLifecycle(pressureState, lifecycle);
        var pressureFlagsCleared = !pressureState.QueryDateRangeClamped
                                   && !pressureState.QueryPageSizeClamped
                                   && !pressureState.ForensicExportTruncated;

        var window = OperationalPressureRecoveryWindowClassifier.Classify(
            telemetry,
            lifecycle,
            !pressureFlagsCleared);

        return new OperationalPressureRecoveryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            RecoveryClassification = recovery.ToString(),
            LifecycleState = lifecycleState.ToString(),
            PressureRecoveryCycles = telemetry.PressureRecoveryCycles,
            StickyPressureRecoveries = telemetry.StickyPressureRecoveries,
            AdaptiveTtlRecoveries = telemetry.AdaptiveTtlRecoveries,
            PressureFlagsCleared = pressureFlagsCleared,
            StabilizationWindow = window,
            ReasonCodes = OperationalPressureExplainabilityBuilder.BuildPressureReasonCodes(
                lifecycleState,
                recovery,
                lifecycle.StickyPressureDetected,
                pressureFlagsCleared,
                100),
            TriggerSignals = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                $"Recovery:{recovery}",
                $"Window:{window.WindowClassification}",
                telemetry.StickyPressureRecoveries > 0 ? "StickyPressureRecovered" : string.Empty
            }),
            GovernanceNotes = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                OperationalPressureGovernance.GetResetAssumption()
            })
        };
    }

    public static OperationalPressureConvergenceDto BuildConvergence(
        IOperationalResiliencePressureState pressureState,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalPressureLifecycleSnapshot lifecycle,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability)
    {
        var (convergenceClass, score) = OperationalPressureConvergenceClassifier.Classify(
            pressureState,
            telemetry,
            lifecycle,
            overview,
            stability);

        var lifecycleState = OperationalPressureStabilizationBuilder.ClassifyLifecycle(pressureState, lifecycle);
        var recovery = OperationalPressureStabilizationBuilder.ClassifyRecovery(pressureState, telemetry, lifecycle);
        var pressureFlagsCleared = !pressureState.QueryDateRangeClamped
                                   && !pressureState.QueryPageSizeClamped
                                   && !pressureState.ForensicExportTruncated;

        var window = OperationalPressureRecoveryWindowClassifier.Classify(
            telemetry,
            lifecycle,
            !pressureFlagsCleared);

        return new OperationalPressureConvergenceDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ConvergenceClassification = convergenceClass,
            ConvergenceScore = score,
            PressureConvergenceRecoveries = telemetry.PressureConvergenceRecoveries,
            PressureLifecycleTransitions = telemetry.PressureLifecycleTransitions,
            StickyPressureDetected = lifecycle.StickyPressureDetected,
            ReadinessState = overview.ReadinessState.ToString(),
            PressureSeverity = overview.PressureSeverity.ToString(),
            StabilizationWindow = window,
            ReasonCodes = OperationalPressureExplainabilityBuilder.BuildPressureReasonCodes(
                lifecycleState,
                recovery,
                lifecycle.StickyPressureDetected,
                pressureFlagsCleared,
                score),
            TriggerSignals = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                $"Convergence:{convergenceClass}",
                $"Score:{score}",
                $"Stability:{stability.StabilityClassification}"
            }),
            GovernanceNotes = OperationalPressureExplainabilityBuilder.Bound(new[]
            {
                OperationalPressureGovernance.GetAssumption(),
                OperationalPressureGovernance.GetNonGoalStatement()
            })
        };
    }
}
