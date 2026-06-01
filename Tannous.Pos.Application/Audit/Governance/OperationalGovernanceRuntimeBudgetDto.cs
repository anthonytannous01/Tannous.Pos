namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceRuntimeBudgetDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int MaxExplainabilitySignals { get; init; }
    public int MaxGovernanceRecommendations { get; init; }
    public int MaxProjectionCollaborators { get; init; }
    public int MaxPipelineDepth { get; init; }
    public int MaxTelemetryCategories { get; init; }
    public int MaxStaleRiskProjections { get; init; }
    public int MaxSurvivabilityScopeEntries { get; init; }
    public int MaxConsistencySignals { get; init; }
    public int EffectiveExplainabilityCap { get; init; }
    public int EffectiveRecommendationCap { get; init; }
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
