namespace Tannous.Pos.Application.Audit;

public sealed class OperationalDiagnosticsCacheDiagnosticsStaleRiskDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int AgingEntryCount { get; init; }
    public int NearExpiryEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> AtRiskEntries { get; init; } =
        Array.Empty<OperationalDiagnosticsCacheEntryMetadataDto>();
}
