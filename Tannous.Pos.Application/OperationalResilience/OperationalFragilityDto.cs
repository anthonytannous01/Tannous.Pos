namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic operational fragility signal.</summary>
public sealed class OperationalFragilityDto
{
    public string FragilityId { get; init; } = string.Empty;
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalFragilityType FragilityType { get; init; }
    public OperationalDurabilityStrength FragilitySeverity { get; init; }
    public string CollapseSensitivity { get; init; } = string.Empty;
    public string EscalationExposure { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
}
