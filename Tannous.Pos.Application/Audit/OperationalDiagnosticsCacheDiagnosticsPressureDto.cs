namespace Tannous.Pos.Application.Audit;

public sealed class OperationalDiagnosticsCacheDiagnosticsPressureDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public long TotalBypasses { get; init; }
    public bool QueryDateRangeClamped { get; init; }
    public bool QueryPageSizeClamped { get; init; }
    public bool ForensicExportTruncated { get; init; }
    public IReadOnlyDictionary<string, long> BypassesByCategory { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
    public string PressureNote { get; init; } = string.Empty;
}
