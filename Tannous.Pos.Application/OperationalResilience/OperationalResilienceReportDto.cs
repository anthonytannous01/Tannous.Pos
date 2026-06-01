namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic operational resilience and survivability report.</summary>
public sealed class OperationalResilienceReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalSurvivabilityState SurvivabilityState { get; init; }
    public string StabilizationDurability { get; init; } = string.Empty;
    public string EscalationFragility { get; init; } = string.Empty;
    public string RecoverySustainability { get; init; } = string.Empty;
    public string ContainmentStrength { get; init; } = string.Empty;
    public string HighestFragilityArea { get; init; } = string.Empty;
    public IReadOnlyList<OperationalSurvivabilityAnalysisDto> SurvivabilityAnalyses { get; init; } =
        Array.Empty<OperationalSurvivabilityAnalysisDto>();
    public IReadOnlyList<OperationalContainmentDurabilityDto> ContainmentDurabilities { get; init; } =
        Array.Empty<OperationalContainmentDurabilityDto>();
    public OperationalResilienceContinuityDto ResilienceContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string ResilienceNote { get; init; } =
        "Advisory deterministic operational survivability interpretation from bounded cognition continuity. Not chaos engineering, fault injection, or probabilistic failure analysis.";
}
