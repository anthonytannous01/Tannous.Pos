using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalForensicGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string ExportControllerSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditForensicExportController.cs"));

    [Fact]
    public void Forensic_export_routes_under_internal_operational_audit_export()
    {
        var text = ExportControllerSource();
        Assert.Contains("internal/operational-audit/export", text, StringComparison.Ordinal);
        Assert.Contains("conflict/{conflictId:guid}", text, StringComparison.Ordinal);
        Assert.Contains("order/{orderId:guid}", text, StringComparison.Ordinal);
        Assert.Contains("operation/{operationId}", text, StringComparison.Ordinal);
        Assert.Contains("device/{deviceId}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_export_endpoints_are_get_only()
    {
        var text = ExportControllerSource();
        Assert.Contains("[HttpGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_export_requires_admin_authorization()
    {
        var text = ExportControllerSource();
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_dtos_exclude_payload_and_stack_traces()
    {
        foreach (var file in new[]
                 {
                     "OperationalForensicSnapshotDto.cs",
                     "ConflictSnapshotItemDto.cs",
                     "AuditTimelineSnapshotItemDto.cs"
                 })
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", file));
            Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Payload", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Forensic_export_declares_internal_read_only_governance()
    {
        var text = ExportControllerSource();
        Assert.Contains("GOVERNANCE / INTERNAL", text, StringComparison.Ordinal);
        Assert.Contains("Read-only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no payload", text, StringComparison.OrdinalIgnoreCase);
    }
}
