namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic operational signal convergence report.</summary>
public sealed class OperationalConvergenceReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantOperationalNarrative { get; init; } = string.Empty;
    public OperationalConvergenceStrength ConvergenceStrength { get; init; }
    public string DivergencePressure { get; init; } = string.Empty;
    public string StabilizationConfidence { get; init; } = string.Empty;
    public string EscalationConfidence { get; init; } = string.Empty;
    public string HighestAmbiguityArea { get; init; } = string.Empty;
    public IReadOnlyList<OperationalSignalReinforcementDto> Reinforcements { get; init; } =
        Array.Empty<OperationalSignalReinforcementDto>();
    public IReadOnlyList<OperationalAmbiguityAnalysisDto> Ambiguities { get; init; } =
        Array.Empty<OperationalAmbiguityAnalysisDto>();
    public OperationalConvergenceContinuityDto ConvergenceContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string ConvergenceNote { get; init; } =
        "Advisory deterministic signal convergence interpretation from bounded operational continuity. Not probabilistic scoring, AI trust analysis, or statistical forecasting.";
}
