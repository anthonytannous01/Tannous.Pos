namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceExecutionDiagnosticsBuilder
{
    public static OperationalGovernanceExecutionDiagnosticsDto Build(
        OperationalGovernanceCompositionContext context,
        OperationalGovernanceExecutionState executionState,
        OperationalGovernanceBudgetPressure budgetPressure,
        OperationalGovernanceProjectionComplexity complexity)
    {
        var reasonCodes = new List<string>
        {
            $"Execution{executionState}",
            $"BudgetPressure{budgetPressure}",
            $"Complexity{complexity}"
        };

        if (context.Telemetry.TotalBypasses > 0)
            reasonCodes.Add("BypassPressureObserved");
        if (context.Telemetry.TotalInvalidations >= 3)
            reasonCodes.Add("InvalidationChurnVisible");

        var effectiveCap = OperationalGovernanceRuntimeBudget.GetEffectiveExplainabilityCap(
            executionState,
            OperationalGovernanceProfileSettings.Default);

        return new OperationalGovernanceExecutionDiagnosticsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ExecutionState = executionState.ToString(),
            BudgetPressure = budgetPressure.ToString(),
            ProjectionComplexity = complexity.ToString(),
            StabilityScore = context.Stability.StabilityScore,
            PressureSeverity = context.Overview.PressureSeverity.ToString(),
            TotalInvalidations = context.Telemetry.TotalInvalidations,
            TotalBypasses = context.Telemetry.TotalBypasses,
            ActiveTelemetryCategories = context.Telemetry.ByCategory.Count,
            ReasonCodes = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
                reasonCodes,
                effectiveCap),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Execution diagnostics are advisory only; no business-path throttling.",
                executionState == OperationalGovernanceExecutionState.Failsafe
                    ? "Failsafe mode constrains governance verbosity only."
                    : string.Empty
            }, 3)
        };
    }
}
