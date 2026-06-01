namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic operational ambiguity analysis for a single area.</summary>
public sealed class OperationalAmbiguityAnalysisDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public string AmbiguitySource { get; init; } = string.Empty;
    public OperationalConvergenceStrength SignalAgreementLevel { get; init; }
    public string StabilizationUncertainty { get; init; } = string.Empty;
    public string EscalationUncertainty { get; init; } = string.Empty;
    public string RecoveryUncertainty { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
