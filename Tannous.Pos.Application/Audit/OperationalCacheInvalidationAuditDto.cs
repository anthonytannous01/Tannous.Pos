namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheInvalidationAuditDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string InvalidationSeverity { get; init; } = string.Empty;
    public string FreshnessRecoveryState { get; init; } = string.Empty;
    public string InvalidationDriftClassification { get; init; } = string.Empty;
    public long TotalInvalidations { get; init; }
    public long ScopedInvalidations { get; init; }
    public long CrossCategoryInvalidations { get; init; }
    public long ScopedInvalidationRecoveries { get; init; }
    public long FreshnessRecoveryCount { get; init; }
    public long InvalidationDriftCount { get; init; }
    public long InvalidationPressureEscalations { get; init; }
    public int ActiveEntryCount { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public int AgingEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public DateTime? LastInvalidationUtc { get; init; }
    public IReadOnlyList<string> AffectedCategories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OperationalCacheInvalidationRecommendationDto> Recommendations { get; init; } =
        Array.Empty<OperationalCacheInvalidationRecommendationDto>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
