namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceProjectionComplexityClassifier
{
    public static OperationalGovernanceProjectionComplexity Classify(
        OperationalGovernanceCompositionContext context,
        int collaboratorFanout)
    {
        var explainabilityEstimate = EstimateExplainabilityCount(context);
        var staleRiskCount = context.StaleRisk.AtRiskEntries.Count;
        var categoryCount = context.Telemetry.ByCategory.Count;
        var scopedKeys = context.Cardinality.ActiveScopedKeyCount;
        var invalidationChurn = context.Telemetry.TotalInvalidations;

        var score = 0;
        if (collaboratorFanout >= OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators)
            score += 2;
        else if (collaboratorFanout >= 4)
            score += 1;

        if (explainabilityEstimate >= 6)
            score += 2;
        else if (explainabilityEstimate >= 3)
            score += 1;

        if (categoryCount >= OperationalGovernanceRuntimeBudget.MaxTelemetryCategories)
            score += 2;
        else if (categoryCount >= 6)
            score += 1;

        if (staleRiskCount >= OperationalGovernanceRuntimeBudget.MaxStaleRiskProjections / 2)
            score += 1;

        if (invalidationChurn >= 10)
            score += 1;

        if (scopedKeys >= OperationalGovernanceRuntimeBudget.MaxSurvivabilityScopeEntries / 2)
            score += 1;

        return score switch
        {
            >= 6 => OperationalGovernanceProjectionComplexity.Extreme,
            >= 4 => OperationalGovernanceProjectionComplexity.Heavy,
            >= 2 => OperationalGovernanceProjectionComplexity.Moderate,
            _ => OperationalGovernanceProjectionComplexity.Minimal
        };
    }

    private static int EstimateExplainabilityCount(OperationalGovernanceCompositionContext context)
    {
        var count = 0;
        if (context.Overview.PressureSeverity >= OperationalCachePressureSeverity.Elevated)
            count++;
        if (context.Telemetry.TotalBypasses > 0)
            count++;
        if (context.Telemetry.RepeatedColdMisses > 0)
            count++;
        if (context.Telemetry.TotalInvalidations >= 3)
            count++;
        if (context.DriftSummary.DriftDetected)
            count += context.DriftSummary.DriftSignals.Count;
        if (context.Stability.StabilityScore < 60)
            count++;
        return count;
    }
}
