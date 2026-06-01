namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Lightweight process-local resilience snapshot for short-term continuity.</summary>
public sealed class OperationalResilienceCognitionSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalSurvivabilityState SurvivabilityState { get; init; }
    public string StabilizationDurability { get; init; } = string.Empty;
    public string EscalationFragility { get; init; } = string.Empty;
    public string HighestFragilityArea { get; init; } = string.Empty;
    public int FragilityCount { get; init; }
}
