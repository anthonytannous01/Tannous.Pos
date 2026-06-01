using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ForensicSurvivabilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Forensic_snapshot_includes_survivability_metadata_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotDto.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("SnapshotGeneratedUtc", text, StringComparison.Ordinal);
        Assert.Contains("SnapshotSchemaVersion", text, StringComparison.Ordinal);
        Assert.Contains("ExportSource", text, StringComparison.Ordinal);
        Assert.Contains("TruncationFlags", text, StringComparison.Ordinal);
        Assert.Contains("RetentionClassification", text, StringComparison.Ordinal);
        Assert.Contains("not legal evidence", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Forensic_service_logs_export_survivability_truncation()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational export survivability:", text, StringComparison.Ordinal);
        Assert.Contains("forensic snapshot truncated", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Retention_observability_anchors_exist()
    {
        var summary = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalRetentionSummaryService.cs"));
        var retention = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditRetentionController.cs"));
        Assert.Contains("Operational retention observability:", summary, StringComparison.Ordinal);
        Assert.Contains("Operational retention observability:", retention, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_conflict_dto_includes_aging_enrichment_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncConflictItemDto.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("AgingSeverity", text, StringComparison.Ordinal);
        Assert.Contains("EscalationRecommendation", text, StringComparison.Ordinal);
    }
}
