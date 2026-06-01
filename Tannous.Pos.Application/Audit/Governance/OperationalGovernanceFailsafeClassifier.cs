namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceFailsafeClassifier
{
    public static bool IsFailsafeActive(
        OperationalGovernanceBudgetPressure budgetPressure,
        OperationalGovernanceTelemetrySaturationLevel saturation,
        OperationalCacheStabilityDto stability,
        OperationalGovernanceExecutionState executionState) =>
        executionState == OperationalGovernanceExecutionState.Failsafe
        || (budgetPressure == OperationalGovernanceBudgetPressure.Critical
            && saturation == OperationalGovernanceTelemetrySaturationLevel.Saturated
            && stability.StabilityScore < 40);

    public static OperationalGovernanceExecutionState ClassifyExecutionState(
        OperationalGovernanceBudgetPressure budgetPressure,
        OperationalGovernanceTelemetrySaturationLevel saturation,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheStabilityDto stability)
    {
        var bypassRatio = OperationalGovernanceThresholdEvaluator.ComputeBypassRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses,
            telemetry.TotalBypasses);

        if (budgetPressure == OperationalGovernanceBudgetPressure.Critical
            && saturation == OperationalGovernanceTelemetrySaturationLevel.Saturated
            && (stability.StabilityScore < 40 || telemetry.ConsistencyConfidenceDrops > 0))
            return OperationalGovernanceExecutionState.Failsafe;

        if (budgetPressure >= OperationalGovernanceBudgetPressure.High
            && saturation == OperationalGovernanceTelemetrySaturationLevel.Saturated)
            return OperationalGovernanceExecutionState.Saturated;

        if (telemetry.TotalInvalidations >= 5
            || bypassRatio >= 0.3
            || telemetry.RepeatedColdMisses >= 3)
            return OperationalGovernanceExecutionState.Constrained;

        return OperationalGovernanceExecutionState.Healthy;
    }
}
