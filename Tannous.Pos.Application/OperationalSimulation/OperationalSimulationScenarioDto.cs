namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Deterministic hypothetical operational scenario.</summary>
public sealed class OperationalSimulationScenarioDto
{
    public string ScenarioId { get; init; } = string.Empty;
    public OperationalSimulationScenarioType ScenarioType { get; init; }
    public string TargetArea { get; init; } = string.Empty;
    public string TriggerCondition { get; init; } = string.Empty;
    public OperationalSimulationDirection ExpectedOperationalDirection { get; init; }
    public string RecoveryImpact { get; init; } = string.Empty;
    public string EscalationImpact { get; init; } = string.Empty;
    public OperationalSimulationSeverity StabilizationLikelihood { get; init; }
    public OperationalSimulationConfidence Confidence { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
}
