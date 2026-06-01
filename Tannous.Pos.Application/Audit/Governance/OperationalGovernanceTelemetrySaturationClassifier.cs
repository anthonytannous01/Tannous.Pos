namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceTelemetrySaturationClassifier
{
    public static OperationalGovernanceTelemetrySaturationLevel Classify(
        OperationalGovernanceCompositionContext context)
    {
        var categories = context.Telemetry.ByCategory.Count;
        var scopedKeys = context.Cardinality.ActiveScopedKeyCount;
        var invalidations = context.Telemetry.TotalInvalidations;
        var projectionBreadth = categories + (context.StaleRisk.AtRiskEntries.Count > 0 ? 1 : 0);

        if (categories >= OperationalGovernanceRuntimeBudget.MaxTelemetryCategories
            || scopedKeys >= OperationalGovernanceRuntimeBudget.MaxSurvivabilityScopeEntries
            || invalidations >= 20)
            return OperationalGovernanceTelemetrySaturationLevel.Saturated;

        if (categories >= 6
            || scopedKeys >= OperationalGovernanceRuntimeBudget.MaxSurvivabilityScopeEntries / 2
            || invalidations >= 8
            || projectionBreadth >= 8)
            return OperationalGovernanceTelemetrySaturationLevel.Elevated;

        return OperationalGovernanceTelemetrySaturationLevel.Nominal;
    }

    public static int ComputeProjectionBreadthScore(OperationalGovernanceCompositionContext context) =>
        context.Telemetry.ByCategory.Count
        + Math.Min(context.StaleRisk.AtRiskEntries.Count, 8)
        + (context.Telemetry.CrossCategoryInvalidations > 0 ? 1 : 0);

    public static int ComputeExplainabilityDensityScore(OperationalGovernanceCompositionContext context)
    {
        var density = 0;
        if (context.Telemetry.TotalBypasses > 0)
            density++;
        if (context.Telemetry.RepeatedColdMisses > 0)
            density++;
        if (context.Telemetry.AdaptiveTtlReductions > 0)
            density++;
        if (context.DriftSummary.DriftSignals.Count > 0)
            density += context.DriftSummary.DriftSignals.Count;
        return density;
    }
}
