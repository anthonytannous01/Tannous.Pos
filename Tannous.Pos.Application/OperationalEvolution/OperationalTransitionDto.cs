namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational state transition between continuity snapshots.</summary>
public sealed class OperationalTransitionDto
{
    public string TransitionId { get; init; } = string.Empty;
    public string SourceState { get; init; } = string.Empty;
    public string TargetState { get; init; } = string.Empty;
    public OperationalTransitionType TransitionType { get; init; }
    public string DominantArea { get; init; } = string.Empty;
    public OperationalEvolutionDirection Direction { get; init; }
    public OperationalEvolutionSeverity Severity { get; init; }
    public string TransitionReason { get; init; } = string.Empty;
    public string OperationalImpact { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
