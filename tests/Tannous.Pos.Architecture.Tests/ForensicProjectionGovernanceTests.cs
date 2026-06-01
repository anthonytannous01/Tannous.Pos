using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ForensicProjectionGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Forensic_service_reuses_metadata_sanitization_projection()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OperationalAuditMetadataProjection.Project", text, StringComparison.Ordinal);
        Assert.Contains("forensic metadata sanitized", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Forensic_service_orders_audit_timeline_chronologically()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderBy(r => r.CreatedAtUtc)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_conflict_projection_includes_resolution_status()
    {
        var dto = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "ConflictSnapshotItemDto.cs"));
        var service = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs"));
        Assert.Contains("ResolutionStatus", dto, StringComparison.Ordinal);
        Assert.Contains("reconciliationStatuses", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_dto_includes_required_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotDto.cs");
        var text = File.ReadAllText(path);
        foreach (var field in new[]
                 {
                     "GeneratedAtUtc",
                     "CorrelationId",
                     "SnapshotType",
                     "Summary",
                     "ConflictRecords",
                     "AuditTimeline",
                     "Metadata"
                 })
        {
            Assert.Contains(field, text, StringComparison.Ordinal);
        }
    }
}
