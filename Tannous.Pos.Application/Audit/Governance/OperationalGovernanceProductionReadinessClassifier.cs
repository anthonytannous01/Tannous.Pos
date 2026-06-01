namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceProductionReadinessClassifier
{
    public static OperationalGovernanceProductionReadinessDto Classify(
        OperationalGovernanceCompositionContext context,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        double snapshotReuseRatio,
        long fingerprintTransitions)
    {
        var hitRatio = context.Overview.HitRatio;
        var hitRatioStability = ComputeHitRatioStabilityScore(hitRatio, context.Stability.StabilityScore);
        var state = ClassifyState(
            context,
            telemetry,
            hitRatioStability,
            snapshotReuseRatio,
            fingerprintTransitions);

        var signals = BuildSignals(state, context, telemetry, fingerprintTransitions);

        return new OperationalGovernanceProductionReadinessDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ReadinessState = state.ToString(),
            HitRatioStabilityScore = hitRatioStability,
            BypassPressureEvents = telemetry.TotalBypasses,
            InvalidationChurn = context.Stability.InvalidationChurn,
            RuntimeFailsafeActivations = telemetry.GovernanceFailsafeActivations,
            SnapshotReuseRatio = snapshotReuseRatio,
            FingerprintTransitions = fingerprintTransitions,
            ReadinessSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Production readiness is advisory only.",
                "Not deployment gating or auto-remediation."
            }, 2)
        };
    }

    private static OperationalGovernanceProductionReadinessState ClassifyState(
        OperationalGovernanceCompositionContext context,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        double hitRatioStability,
        double snapshotReuseRatio,
        long fingerprintTransitions)
    {
        if (context.FailsafeActive
            || context.ExecutionState == OperationalGovernanceExecutionState.Failsafe
            || telemetry.GovernanceFailsafeActivations >= 5
            || fingerprintTransitions >= 8
            || context.Overview.PressureSeverity >= OperationalCachePressureSeverity.Critical)
            return OperationalGovernanceProductionReadinessState.GovernanceSaturated;

        if (hitRatioStability >= 70
            && context.Overview.PressureSeverity <= OperationalCachePressureSeverity.Elevated
            && telemetry.TotalBypasses <= 10
            && context.Stability.InvalidationChurn <= 20
            && fingerprintTransitions <= 3)
            return OperationalGovernanceProductionReadinessState.OperationallyStable;

        if (hitRatioStability >= 45
            && snapshotReuseRatio >= 0.2
            && telemetry.GovernanceFailsafeActivations <= 2)
            return OperationalGovernanceProductionReadinessState.IntegrationReady;

        return OperationalGovernanceProductionReadinessState.DevelopmentReady;
    }

    private static double ComputeHitRatioStabilityScore(double hitRatio, int stabilityScore) =>
        Math.Round(Math.Clamp((hitRatio * 50d) + (stabilityScore * 0.5d), 0d, 100d), 1);

    private static IReadOnlyList<string> BuildSignals(
        OperationalGovernanceProductionReadinessState state,
        OperationalGovernanceCompositionContext context,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        long fingerprintTransitions)
    {
        var signals = new List<string> { $"Readiness:{state}" };

        if (telemetry.TotalBypasses > 5)
            signals.Add("BypassPressureElevated");
        if (context.Stability.InvalidationChurn > 10)
            signals.Add("InvalidationChurnElevated");
        if (fingerprintTransitions > 0)
            signals.Add("FingerprintDriftObserved");
        if (telemetry.GovernanceFailsafeActivations > 0)
            signals.Add("RuntimeFailsafeObserved");

        return signals;
    }
}
