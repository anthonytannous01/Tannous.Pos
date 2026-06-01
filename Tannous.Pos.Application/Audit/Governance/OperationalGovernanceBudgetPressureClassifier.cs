namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceBudgetPressureClassifier
{
    public static OperationalGovernanceBudgetPressure Classify(
        OperationalCachePressureSeverity cachePressureSeverity,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        if (cachePressureSeverity == OperationalCachePressureSeverity.Critical
            || telemetry.InvalidationPressureEscalations >= 2)
            return OperationalGovernanceBudgetPressure.Critical;

        if (cachePressureSeverity == OperationalCachePressureSeverity.High
            || telemetry.TotalInvalidations >= OperationalGovernanceRuntimeBudget.MaxConsistencySignals * 2)
            return OperationalGovernanceBudgetPressure.High;

        var bypassRatio = OperationalGovernanceThresholdEvaluator.ComputeBypassRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses,
            telemetry.TotalBypasses);
        if (cachePressureSeverity == OperationalCachePressureSeverity.Elevated
            || bypassRatio >= 0.25
            || telemetry.RepeatedColdMisses >= 2)
            return OperationalGovernanceBudgetPressure.Elevated;

        return OperationalGovernanceBudgetPressure.Nominal;
    }
}
