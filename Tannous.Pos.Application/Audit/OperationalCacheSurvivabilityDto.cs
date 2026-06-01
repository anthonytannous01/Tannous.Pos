namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheSurvivabilityDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int SurvivabilityScore { get; init; }
    public OperationalCacheSurvivabilityClassification Classification { get; init; }
    public string ClassificationLabel { get; init; } = string.Empty;
    public string RecommendedOperatorAction { get; init; } = string.Empty;
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
