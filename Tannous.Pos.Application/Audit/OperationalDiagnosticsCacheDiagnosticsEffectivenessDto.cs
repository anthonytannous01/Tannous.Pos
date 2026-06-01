namespace Tannous.Pos.Application.Audit;

public sealed class OperationalDiagnosticsCacheDiagnosticsEffectivenessDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public long TotalBypasses { get; init; }
    public long TotalStaleServes { get; init; }
    public double HitRatio { get; init; }
    public long TotalInvalidations { get; init; }
    public DateTime? LastInvalidationUtc { get; init; }
    public IReadOnlyDictionary<string, OperationalDiagnosticsCacheCategoryTelemetryDto> ByCategory { get; init; }
        = new Dictionary<string, OperationalDiagnosticsCacheCategoryTelemetryDto>();
}
