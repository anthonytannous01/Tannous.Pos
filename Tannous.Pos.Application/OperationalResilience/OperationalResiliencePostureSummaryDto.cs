namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Condensed operational resilience summary for operator attention.</summary>
public sealed class OperationalResiliencePostureSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantResilienceArea { get; init; } = string.Empty;
    public string HighestFragilityPressure { get; init; } = string.Empty;
    public string StrongestContainmentZone { get; init; } = string.Empty;
    public string WeakestRecoveryDurability { get; init; } = string.Empty;
    public OperationalSurvivabilityState OperationalSurvivabilityState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
