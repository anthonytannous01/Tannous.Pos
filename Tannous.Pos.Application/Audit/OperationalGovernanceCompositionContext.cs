namespace Tannous.Pos.Application.Audit;

using Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Shared computed governance context for projection builders (process-local; not cached or persisted).
/// </summary>
public sealed class OperationalGovernanceCompositionContext
{
    public required IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> Entries { get; init; }

    public required OperationalDiagnosticsCacheTelemetrySnapshotDto Telemetry { get; init; }

    public required OperationalDiagnosticsCacheDiagnosticsStaleRiskDto StaleRisk { get; init; }

    public required OperationalCacheAdaptivePressureSignals PressureSignals { get; init; }

    public required OperationalCacheCardinalitySnapshotDto Cardinality { get; init; }

    public required OperationalCacheGovernanceOverviewDto Overview { get; init; }

    public required OperationalCacheStabilityDto Stability { get; init; }

    public required OperationalCacheSurvivabilityDto Survivability { get; init; }

    public OperationalCacheReadinessState ReadinessState => Overview.ReadinessState;

    public OperationalCachePressureSeverity PressureSeverity => Overview.PressureSeverity;

    public OperationalCacheDegradationState DegradationState => Overview.DegradationState;

    public string InvalidationPressureSeverity { get; init; } = string.Empty;

    public OperationalCacheGovernanceDriftDto DriftSummary { get; init; } = new();

    public OperationalGovernanceExecutionState ExecutionState { get; init; } =
        OperationalGovernanceExecutionState.Healthy;

    public OperationalGovernanceBudgetPressure BudgetPressure { get; init; } =
        OperationalGovernanceBudgetPressure.Nominal;

    public OperationalGovernanceProjectionComplexity ProjectionComplexity { get; init; } =
        OperationalGovernanceProjectionComplexity.Minimal;

    public OperationalGovernanceTelemetrySaturationLevel TelemetrySaturationLevel { get; init; } =
        OperationalGovernanceTelemetrySaturationLevel.Nominal;

    public int EffectiveExplainabilityCap { get; init; } =
        OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals;

    public bool WarmRecommendationsSuppressed { get; init; }

    public bool FailsafeActive { get; init; }
}
