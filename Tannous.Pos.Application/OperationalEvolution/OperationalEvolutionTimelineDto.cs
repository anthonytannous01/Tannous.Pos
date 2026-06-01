namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational evolution timeline from bounded snapshot continuity.</summary>
public sealed class OperationalEvolutionTimelineDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalEvolutionDirection DominantEvolutionDirection { get; init; }
    public int ActiveTransitionCount { get; init; }
    public string RecoveryMomentum { get; init; } = string.Empty;
    public string EscalationMomentum { get; init; } = string.Empty;
    public string StabilizationMomentum { get; init; } = string.Empty;
    public string DominantOperationalShift { get; init; } = string.Empty;
    public IReadOnlyList<OperationalTransitionDto> Transitions { get; init; } = Array.Empty<OperationalTransitionDto>();
    public IReadOnlyList<OperationalEvolutionPhaseDto> Phases { get; init; } = Array.Empty<OperationalEvolutionPhaseDto>();
    public OperationalEvolutionContinuityDto EvolutionContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string EvolutionNote { get; init; } =
        "Advisory deterministic operational evolution from bounded snapshot continuity. Not historical analytics, time-series platforms, or forecasting.";
}
