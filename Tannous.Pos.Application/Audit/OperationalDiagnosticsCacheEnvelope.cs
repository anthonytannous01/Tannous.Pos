namespace Tannous.Pos.Application.Audit;

/// <summary>Cache envelope for diagnostics summaries (not durable; not distributed).</summary>
public sealed class OperationalDiagnosticsCacheEnvelope<T> where T : class
{
    public required T Value { get; init; }
    public required DateTime CreatedUtc { get; init; }
    public required DateTime ExpiresUtc { get; init; }
    public required string Category { get; init; }
    public required string CacheKey { get; init; }
    public DateTime ServedUtc { get; set; }

    public TimeSpan Age => DateTime.UtcNow - CreatedUtc;

    public TimeSpan RemainingTtl =>
        ExpiresUtc > DateTime.UtcNow ? ExpiresUtc - DateTime.UtcNow : TimeSpan.Zero;

    public OperationalDiagnosticsCacheStaleRisk StaleRisk =>
        OperationalDiagnosticsCacheStaleRiskClassifier.Classify(CreatedUtc, ExpiresUtc, DateTime.UtcNow);
}
