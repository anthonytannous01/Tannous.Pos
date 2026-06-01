namespace Tannous.Pos.Application.Audit;

/// <summary>Stale-risk projection reused by invalidation and consistency governance paths.</summary>
public static class OperationalGovernanceStaleRiskProjectionBuilder
{
    public static OperationalDiagnosticsCacheDiagnosticsStaleRiskDto Build(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries)
    {
        var atRisk = entries
            .Where(e => e.StaleRisk != OperationalDiagnosticsCacheStaleRisk.Fresh)
            .ToList();

        return new OperationalDiagnosticsCacheDiagnosticsStaleRiskDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            AgingEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.Aging),
            NearExpiryEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.NearExpiry),
            ExpiredEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.Expired),
            AtRiskEntries = atRisk
        };
    }
}
