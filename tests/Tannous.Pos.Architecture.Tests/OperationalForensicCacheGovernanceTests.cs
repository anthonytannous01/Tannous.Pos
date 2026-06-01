using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalForensicCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Forensic_service_does_not_cache_full_snapshot_dto()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("IOperationalDiagnosticsCache", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<OperationalForensicSnapshotDto>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_service_reuses_cached_upstream_summaries_for_compact_projection()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("BuildCompactForensicSummary", text, StringComparison.Ordinal);
        Assert.Contains("_resilienceDiagnostics.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("_reconciliation.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("_incidentCorrelation.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("CompactSummary = compactSummary", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_forensic_summary_dto_has_no_raw_payload_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotSummaryDto.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OperationalForensicSnapshotSummaryDto", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AuditTimeline", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ConflictRecords", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MetadataJson", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Metadata {", text, StringComparison.Ordinal);
    }
}
