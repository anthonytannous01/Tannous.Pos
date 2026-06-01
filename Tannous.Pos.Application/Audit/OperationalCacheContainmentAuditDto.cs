namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheContainmentAuditDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ContainmentState { get; init; } = string.Empty;
    public string PropagationSeverity { get; init; } = string.Empty;
    public string ConfidenceLevel { get; init; } = string.Empty;
    public long ContainmentEscalations { get; init; }
    public long PropagationDetections { get; init; }
    public long ConsistencyConfidenceDrops { get; init; }
    public int StabilityScore { get; init; }
    public string DegradationState { get; init; } = string.Empty;
    public string PressureSeverity { get; init; } = string.Empty;
    public IReadOnlyList<string> AffectedCategories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OperationalCacheContainmentRecommendationDto> Recommendations { get; init; } =
        Array.Empty<OperationalCacheContainmentRecommendationDto>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
