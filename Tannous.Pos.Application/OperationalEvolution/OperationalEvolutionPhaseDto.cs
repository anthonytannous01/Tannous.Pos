namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational evolution phase interpretation.</summary>
public sealed class OperationalEvolutionPhaseDto
{
    public string PhaseId { get; init; } = string.Empty;
    public string PhaseName { get; init; } = string.Empty;
    public OperationalPhaseType PhaseType { get; init; }
    public string DominantOperationalCondition { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string EscalationAlignment { get; init; } = string.Empty;
    public string StabilizationAlignment { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
