namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Deterministic static projection pipeline (compile-time stages; no reflection/plugins).
/// </summary>
public static class OperationalGovernanceProjectionPipeline
{
    public static IReadOnlyList<string> StageOrder { get; } =
    [
        OperationalGovernanceProjectionStages.TelemetrySnapshot,
        OperationalGovernanceProjectionStages.StaleRisk,
        OperationalGovernanceProjectionStages.Pressure,
        OperationalGovernanceProjectionStages.Invalidation,
        OperationalGovernanceProjectionStages.Consistency,
        OperationalGovernanceProjectionStages.Explainability,
        OperationalGovernanceProjectionStages.Normalization,
        OperationalGovernanceProjectionStages.RuntimeProtection
    ];

    public static OperationalGovernanceCompositionContext Execute(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        IOperationalResiliencePressureState pressureState,
        OperationalGovernanceProfile profile = OperationalGovernanceProfile.Standard)
    {
        var workspace = new PipelineWorkspace(entries, telemetry, pressureState, profile);

        foreach (var stage in StageOrder)
        {
            switch (stage)
            {
                case OperationalGovernanceProjectionStages.TelemetrySnapshot:
                    TelemetrySnapshotStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.StaleRisk:
                    StaleRiskStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.Pressure:
                    PressureStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.Invalidation:
                    InvalidationStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.Consistency:
                    ConsistencyStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.Explainability:
                    ExplainabilityStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.Normalization:
                    NormalizationStage.Apply(workspace);
                    break;
                case OperationalGovernanceProjectionStages.RuntimeProtection:
                    RuntimeProtectionStage.Apply(workspace);
                    break;
            }
        }

        return workspace.Context
               ?? throw new InvalidOperationException("Governance projection pipeline did not produce context.");
    }

    internal sealed class PipelineWorkspace
    {
        public PipelineWorkspace(
            IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
            OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
            IOperationalResiliencePressureState pressureState,
            OperationalGovernanceProfile profile)
        {
            Entries = entries;
            Telemetry = telemetry;
            PressureState = pressureState;
            Profile = profile;
        }

        public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> Entries { get; }
        public OperationalDiagnosticsCacheTelemetrySnapshotDto Telemetry { get; }
        public IOperationalResiliencePressureState PressureState { get; }
        public OperationalGovernanceProfile Profile { get; }

        public OperationalDiagnosticsCacheDiagnosticsStaleRiskDto? StaleRisk { get; set; }
        public OperationalCacheAdaptivePressureSignals? PressureSignals { get; set; }
        public OperationalCacheCardinalitySnapshotDto? Cardinality { get; set; }
        public OperationalCacheGovernanceOverviewDto? Overview { get; set; }
        public OperationalCacheStabilityDto? Stability { get; set; }
        public OperationalCacheSurvivabilityDto? Survivability { get; set; }
        public string InvalidationPressureSeverity { get; set; } = string.Empty;
        public OperationalCacheGovernanceDriftDto? DriftSummary { get; set; }
        public int ExplainabilityCap { get; set; }
        public OperationalGovernanceCompositionContext? Context { get; set; }
    }

    private static class TelemetrySnapshotStage
    {
        public static void Apply(PipelineWorkspace workspace) =>
            _ = workspace.Entries.Count >= 0 && workspace.Telemetry.SnapshotUtc != default;
    }

    private static class StaleRiskStage
    {
        public static void Apply(PipelineWorkspace workspace) =>
            workspace.StaleRisk = OperationalGovernanceStaleRiskProjectionBuilder.Build(workspace.Entries);
    }

    private static class PressureStage
    {
        public static void Apply(PipelineWorkspace workspace)
        {
            workspace.PressureSignals = OperationalCacheAdaptivePressureSignals.FromPressureState(workspace.PressureState);
            workspace.Cardinality = OperationalCacheCardinalityClassifier.BuildSnapshot(workspace.Entries);
            workspace.Overview = OperationalCacheGovernanceProjectionBuilder.BuildOverview(
                workspace.Entries,
                workspace.Telemetry,
                workspace.PressureSignals);
        }
    }

    private static class InvalidationStage
    {
        public static void Apply(PipelineWorkspace workspace)
        {
            var invalidationPressure = OperationalCacheInvalidationProjectionBuilder.BuildPressure(
                workspace.Entries,
                workspace.Telemetry);
            workspace.InvalidationPressureSeverity = invalidationPressure.InvalidationSeverity;
        }
    }

    private static class ConsistencyStage
    {
        public static void Apply(PipelineWorkspace workspace)
        {
            workspace.Stability = OperationalCacheGovernanceAuditBuilder.EnrichStability(
                OperationalCacheStabilityClassifier.Compute(workspace.Telemetry),
                workspace.Overview!,
                workspace.Telemetry);
            workspace.Survivability = OperationalCacheSurvivabilityClassifier.Compute(
                workspace.Telemetry,
                workspace.Overview!,
                workspace.Stability);
            workspace.DriftSummary = OperationalCacheGovernanceDriftDetector.Detect(
                workspace.Overview!,
                workspace.Telemetry,
                workspace.Stability);
        }
    }

    private static class ExplainabilityStage
    {
        public static void Apply(PipelineWorkspace workspace) =>
            workspace.ExplainabilityCap = OperationalGovernanceProfileSettings.GetExplainabilityCap(workspace.Profile);
    }

    private static class RuntimeProtectionStage
    {
        public static void Apply(PipelineWorkspace workspace)
        {
            if (workspace.Context == null)
                return;

            var budgetPressure = OperationalGovernanceBudgetPressureClassifier.Classify(
                workspace.Context.Overview.PressureSeverity,
                workspace.Context.Telemetry);
            var saturationLevel = OperationalGovernanceTelemetrySaturationClassifier.Classify(workspace.Context);
            var executionState = OperationalGovernanceFailsafeClassifier.ClassifyExecutionState(
                budgetPressure,
                saturationLevel,
                workspace.Context.Telemetry,
                workspace.Context.Stability);
            var complexity = OperationalGovernanceProjectionComplexityClassifier.Classify(
                workspace.Context,
                OperationalGovernanceRuntimeProtectionBuilder.DefaultCollaboratorFanout);
            var failsafeActive = OperationalGovernanceFailsafeClassifier.IsFailsafeActive(
                budgetPressure,
                saturationLevel,
                workspace.Context.Stability,
                executionState);
            var effectiveCap = OperationalGovernanceRuntimeBudget.GetEffectiveExplainabilityCap(
                executionState,
                workspace.Profile);

            workspace.Context = new OperationalGovernanceCompositionContext
            {
                Entries = workspace.Context.Entries,
                Telemetry = workspace.Context.Telemetry,
                StaleRisk = workspace.Context.StaleRisk,
                PressureSignals = workspace.Context.PressureSignals,
                Cardinality = workspace.Context.Cardinality,
                Overview = workspace.Context.Overview,
                Stability = workspace.Context.Stability,
                Survivability = workspace.Context.Survivability,
                InvalidationPressureSeverity = workspace.Context.InvalidationPressureSeverity,
                DriftSummary = workspace.Context.DriftSummary,
                ExecutionState = executionState,
                BudgetPressure = budgetPressure,
                ProjectionComplexity = complexity,
                TelemetrySaturationLevel = saturationLevel,
                EffectiveExplainabilityCap = effectiveCap,
                WarmRecommendationsSuppressed = failsafeActive,
                FailsafeActive = failsafeActive
            };
        }
    }

    private static class NormalizationStage
    {
        public static void Apply(PipelineWorkspace workspace)
        {
            workspace.Context = new OperationalGovernanceCompositionContext
            {
                Entries = workspace.Entries,
                Telemetry = workspace.Telemetry,
                StaleRisk = workspace.StaleRisk!,
                PressureSignals = workspace.PressureSignals!,
                Cardinality = workspace.Cardinality!,
                Overview = workspace.Overview!,
                Stability = workspace.Stability!,
                Survivability = workspace.Survivability!,
                InvalidationPressureSeverity = workspace.InvalidationPressureSeverity,
                DriftSummary = workspace.DriftSummary!
            };
        }
    }
}

public static class OperationalGovernanceProjectionStages
{
    public const string TelemetrySnapshot = "TelemetrySnapshot";
    public const string StaleRisk = "StaleRisk";
    public const string Pressure = "Pressure";
    public const string Invalidation = "Invalidation";
    public const string Consistency = "Consistency";
    public const string Explainability = "Explainability";
    public const string Normalization = "Normalization";
    public const string RuntimeProtection = "RuntimeProtection";
}
