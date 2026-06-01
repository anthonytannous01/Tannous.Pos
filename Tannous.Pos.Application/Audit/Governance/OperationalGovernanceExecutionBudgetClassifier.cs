namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceExecutionBudgetClassifier
{
    public const int FastBuildThresholdMs = 25;
    public const int ModerateBuildThresholdMs = 75;
    public const int SlowBuildThresholdMs = 150;

    public static OperationalGovernanceProjectionTimingBand ClassifyTimingBand(int buildElapsedMilliseconds)
    {
        if (buildElapsedMilliseconds <= FastBuildThresholdMs)
            return OperationalGovernanceProjectionTimingBand.Fast;

        if (buildElapsedMilliseconds <= ModerateBuildThresholdMs)
            return OperationalGovernanceProjectionTimingBand.Moderate;

        if (buildElapsedMilliseconds <= SlowBuildThresholdMs)
            return OperationalGovernanceProjectionTimingBand.Slow;

        return OperationalGovernanceProjectionTimingBand.Elevated;
    }

    public static OperationalGovernanceExecutionBudgetState Classify(
        OperationalGovernanceCompositionContext context,
        OperationalGovernanceProjectionTimingBand timingBand,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        if (context.FailsafeActive
            || context.ExecutionState == OperationalGovernanceExecutionState.Failsafe
            || telemetry.GovernanceFailsafeActivations >= 3)
            return OperationalGovernanceExecutionBudgetState.Critical;

        if (context.ExecutionState is OperationalGovernanceExecutionState.Saturated
            || context.BudgetPressure >= OperationalGovernanceBudgetPressure.High
            || timingBand == OperationalGovernanceProjectionTimingBand.Elevated
            || telemetry.RuntimeBudgetConstrainedEvents >= 5)
            return OperationalGovernanceExecutionBudgetState.Constrained;

        if (context.ExecutionState == OperationalGovernanceExecutionState.Constrained
            || context.BudgetPressure == OperationalGovernanceBudgetPressure.Elevated
            || timingBand == OperationalGovernanceProjectionTimingBand.Slow
            || telemetry.ExplainabilityTruncations > 0)
            return OperationalGovernanceExecutionBudgetState.Elevated;

        return OperationalGovernanceExecutionBudgetState.Nominal;
    }
}
