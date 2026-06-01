namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceRuntimeProtectionBuilder
{
    public const int DefaultCollaboratorFanout = 8;

    public static OperationalGovernanceRuntimeProtectionDto Build(
        OperationalGovernanceCompositionContext context,
        OperationalGovernanceProfile profile,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        int collaboratorFanout = DefaultCollaboratorFanout)
    {
        var executionState = context.ExecutionState;
        var budgetPressure = context.BudgetPressure;
        var saturationLevel = context.TelemetrySaturationLevel;
        var complexity = context.ProjectionComplexity;
        var failsafeActive = context.FailsafeActive;
        var effectiveExplainabilityCap = context.EffectiveExplainabilityCap;
        var effectiveRecommendationCap = OperationalGovernanceRuntimeBudget.GetEffectiveRecommendationCap(
            executionState);

        var explainabilityCodes = BuildExplainabilityCodes(
            executionState,
            budgetPressure,
            saturationLevel,
            complexity,
            failsafeActive,
            effectiveExplainabilityCap,
            telemetry);

        var recommendations = BuildRecommendations(
            executionState,
            failsafeActive,
            effectiveRecommendationCap);

        var snapshot = telemetry.GetSnapshot();

        return new OperationalGovernanceRuntimeProtectionDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ExecutionState = executionState.ToString(),
            BudgetPressure = budgetPressure.ToString(),
            ProjectionComplexity = complexity.ToString(),
            TelemetrySaturationLevel = saturationLevel.ToString(),
            Budget = BuildBudgetDto(executionState, profile, effectiveExplainabilityCap, effectiveRecommendationCap),
            ExecutionDiagnostics = OperationalGovernanceExecutionDiagnosticsBuilder.Build(
                context,
                executionState,
                budgetPressure,
                complexity),
            TelemetrySaturation = BuildTelemetrySaturation(context, snapshot),
            Failsafe = BuildFailsafeDto(
                failsafeActive,
                executionState,
                snapshot,
                explainabilityCodes.Count < 6 && failsafeActive),
            ExplainabilityCodes = explainabilityCodes,
            ProtectionRecommendations = recommendations,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Runtime protection is process-local and non-authoritative.",
                "Failsafe does not disable diagnostics endpoints.",
                failsafeActive ? "Warm recommendations suppressed in advisory projections." : string.Empty
            }, 3)
        };
    }

    public static OperationalGovernanceExecutionDiagnosticsDto BuildExecutionDiagnostics(
        OperationalGovernanceCompositionContext context) =>
        OperationalGovernanceExecutionDiagnosticsBuilder.Build(
            context,
            context.ExecutionState,
            context.BudgetPressure,
            context.ProjectionComplexity);

    public static OperationalGovernanceTelemetrySaturationDto BuildTelemetrySaturation(
        OperationalGovernanceCompositionContext context,
        OperationalDiagnosticsCacheTelemetrySnapshotDto snapshot)
    {
        var signals = new List<string> { $"Saturation{context.TelemetrySaturationLevel}" };
        if (context.Telemetry.ByCategory.Count >= 6)
            signals.Add("CategoryBreadthElevated");
        if (context.Cardinality.ActiveScopedKeyCount >= 16)
            signals.Add("ScopedKeyBreadthElevated");
        if (context.Telemetry.TotalInvalidations >= 8)
            signals.Add("InvalidationChurnElevated");

        return new OperationalGovernanceTelemetrySaturationDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SaturationLevel = context.TelemetrySaturationLevel.ToString(),
            ActiveTelemetryCategories = context.Telemetry.ByCategory.Count,
            ActiveScopedKeyCount = context.Cardinality.ActiveScopedKeyCount,
            TotalInvalidations = context.Telemetry.TotalInvalidations,
            ProjectionBreadthScore = OperationalGovernanceTelemetrySaturationClassifier.ComputeProjectionBreadthScore(context),
            ExplainabilityDensityScore = OperationalGovernanceTelemetrySaturationClassifier.ComputeExplainabilityDensityScore(context),
            TelemetrySaturationEvents = snapshot.TelemetrySaturationEvents,
            SaturationSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 8),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Telemetry saturation is advisory; no historical sampling or persistence."
            }, 2)
        };
    }

    private static OperationalGovernanceRuntimeBudgetDto BuildBudgetDto(
        OperationalGovernanceExecutionState executionState,
        OperationalGovernanceProfile profile,
        int effectiveExplainabilityCap,
        int effectiveRecommendationCap) =>
        new()
        {
            GeneratedAtUtc = DateTime.UtcNow,
            MaxExplainabilitySignals = OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals,
            MaxGovernanceRecommendations = OperationalGovernanceRuntimeBudget.MaxGovernanceRecommendations,
            MaxProjectionCollaborators = OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators,
            MaxPipelineDepth = OperationalGovernanceRuntimeBudget.MaxPipelineDepth,
            MaxTelemetryCategories = OperationalGovernanceRuntimeBudget.MaxTelemetryCategories,
            MaxStaleRiskProjections = OperationalGovernanceRuntimeBudget.MaxStaleRiskProjections,
            MaxSurvivabilityScopeEntries = OperationalGovernanceRuntimeBudget.MaxSurvivabilityScopeEntries,
            MaxConsistencySignals = OperationalGovernanceRuntimeBudget.MaxConsistencySignals,
            EffectiveExplainabilityCap = effectiveExplainabilityCap,
            EffectiveRecommendationCap = effectiveRecommendationCap,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                $"Profile:{profile}",
                $"Execution:{executionState}",
                "Budget enforcement is projection-time only."
            }, 3)
        };

    private static OperationalGovernanceFailsafeDto BuildFailsafeDto(
        bool failsafeActive,
        OperationalGovernanceExecutionState executionState,
        OperationalDiagnosticsCacheTelemetrySnapshotDto snapshot,
        bool explainabilityTruncated) =>
        new()
        {
            GeneratedAtUtc = DateTime.UtcNow,
            FailsafeActive = failsafeActive,
            WarmRecommendationsSuppressed = failsafeActive,
            ExplainabilityTruncated = explainabilityTruncated,
            RecommendationsReduced = failsafeActive,
            GovernanceFailsafeActivations = snapshot.GovernanceFailsafeActivations,
            ProtectionSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(new[]
            {
                failsafeActive ? "FailsafeActive" : "FailsafeInactive",
                executionState == OperationalGovernanceExecutionState.Failsafe ? "ExecutionFailsafe" : string.Empty
            }, 4),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Failsafe does not disable diagnostics endpoints.",
                "No automatic recovery or remediation is performed."
            }, 2)
        };

    private static IReadOnlyList<string> BuildExplainabilityCodes(
        OperationalGovernanceExecutionState executionState,
        OperationalGovernanceBudgetPressure budgetPressure,
        OperationalGovernanceTelemetrySaturationLevel saturationLevel,
        OperationalGovernanceProjectionComplexity complexity,
        bool failsafeActive,
        int effectiveCap,
        IOperationalDiagnosticsCacheTelemetry telemetry)
    {
        var raw = new[]
        {
            $"Execution{executionState}",
            $"BudgetPressure{budgetPressure}",
            $"Saturation{saturationLevel}",
            $"Complexity{complexity}",
            failsafeActive ? "FailsafeEngaged" : string.Empty,
            failsafeActive ? "WarmRecommendationsSuppressed" : string.Empty
        };

        var distinctRaw = raw.Count(s => !string.IsNullOrWhiteSpace(s));
        var composed = OperationalGovernanceExplainabilityComposer.ComposeWithRuntimeCap(
            raw,
            effectiveCap,
            orderDeterministically: true);

        if (composed.Count < distinctRaw)
            telemetry.RecordExplainabilityTruncation();

        return composed;
    }

    private static IReadOnlyList<string> BuildRecommendations(
        OperationalGovernanceExecutionState executionState,
        bool failsafeActive,
        int effectiveRecommendationCap)
    {
        var items = new List<string>();
        if (executionState == OperationalGovernanceExecutionState.Constrained)
            items.Add("Reduce repeated diagnostics queries to lower governance churn.");
        if (executionState == OperationalGovernanceExecutionState.Saturated)
            items.Add("Review cache pressure indicators; expect advisory-only degraded readiness.");
        if (failsafeActive)
            items.Add("Governance projections are constrained; endpoints remain available.");

        if (items.Count == 0)
            items.Add("No runtime protection action required.");

        return OperationalGovernanceRuntimeBudget.ClampOrdered(items, effectiveRecommendationCap);
    }
}
