namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Short-term resilience continuity from bounded snapshots.</summary>
public sealed class OperationalResilienceContinuityDto
{
    public string DominantResilienceShift { get; init; } = string.Empty;
    public string SurvivabilityConsistency { get; init; } = string.Empty;
    public string FragilityConsistency { get; init; } = string.Empty;
    public string RecoveryDurabilityAlignment { get; init; } = string.Empty;
    public string EscalationResistanceAlignment { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
