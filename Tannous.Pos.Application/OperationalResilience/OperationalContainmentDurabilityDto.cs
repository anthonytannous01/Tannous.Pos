namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic containment durability interpretation.</summary>
public sealed class OperationalContainmentDurabilityDto
{
    public string ContainmentArea { get; init; } = string.Empty;
    public OperationalDurabilityStrength DurabilityStrength { get; init; }
    public string StabilizationConsistency { get; init; } = string.Empty;
    public string EscalationContainmentStrength { get; init; } = string.Empty;
    public string RecoverySupportStrength { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
