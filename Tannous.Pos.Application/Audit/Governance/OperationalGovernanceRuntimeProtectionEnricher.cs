namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Enriches runtime protection projections with advisory baselines without altering core fields.</summary>
public static class OperationalGovernanceRuntimeProtectionEnricher
{
    public static OperationalGovernanceRuntimeProtectionDto Enrich(
        OperationalGovernanceRuntimeProtectionDto source,
        OperationalGovernanceRuntimeBaselineDto runtimeBaseline,
        OperationalGovernanceProductionReadinessDto productionReadiness) =>
        new()
        {
            GeneratedAtUtc = source.GeneratedAtUtc,
            ExecutionState = source.ExecutionState,
            BudgetPressure = source.BudgetPressure,
            ProjectionComplexity = source.ProjectionComplexity,
            TelemetrySaturationLevel = source.TelemetrySaturationLevel,
            Budget = source.Budget,
            ExecutionDiagnostics = source.ExecutionDiagnostics,
            TelemetrySaturation = source.TelemetrySaturation,
            Failsafe = source.Failsafe,
            ExplainabilityCodes = source.ExplainabilityCodes,
            ProtectionRecommendations = source.ProtectionRecommendations,
            GovernanceNotes = source.GovernanceNotes,
            RuntimeBaseline = runtimeBaseline,
            ProductionReadiness = productionReadiness
        };
}
