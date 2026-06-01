namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceRuntimeProtectionDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ExecutionState { get; init; } = string.Empty;
    public string BudgetPressure { get; init; } = string.Empty;
    public string ProjectionComplexity { get; init; } = string.Empty;
    public string TelemetrySaturationLevel { get; init; } = string.Empty;
    public OperationalGovernanceRuntimeBudgetDto Budget { get; init; } = new();
    public OperationalGovernanceExecutionDiagnosticsDto ExecutionDiagnostics { get; init; } = new();
    public OperationalGovernanceTelemetrySaturationDto TelemetrySaturation { get; init; } = new();
    public OperationalGovernanceFailsafeDto Failsafe { get; init; } = new();
    public IReadOnlyList<string> ExplainabilityCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ProtectionRecommendations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public OperationalGovernanceRuntimeBaselineDto RuntimeBaseline { get; init; } = new();
    public OperationalGovernanceProductionReadinessDto ProductionReadiness { get; init; } = new();
}
