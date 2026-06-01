namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic survivability analysis for a single operational area.</summary>
public sealed class OperationalSurvivabilityAnalysisDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalDurabilityStrength SurvivabilityStrength { get; init; }
    public string StabilizationResistance { get; init; } = string.Empty;
    public string EscalationResistance { get; init; } = string.Empty;
    public string DependencyDurability { get; init; } = string.Empty;
    public string RecoveryDurability { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
