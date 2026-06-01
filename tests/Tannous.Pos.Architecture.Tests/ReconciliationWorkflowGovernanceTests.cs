using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ReconciliationWorkflowGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string ReconciliationControllerSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditReconciliationController.cs"));

    [Fact]
    public void Reconciliation_resolution_status_enum_is_stable()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Enums", "ReconciliationResolutionStatus.cs");
        var text = File.ReadAllText(path);
        foreach (var status in new[] { "Unresolved", "Acknowledged", "Investigating", "Resolved", "Ignored" })
            Assert.Contains(status, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_endpoints_exist_under_internal_operational_audit()
    {
        var text = ReconciliationControllerSource();
        Assert.Contains("internal/operational-audit/reconciliation", text, StringComparison.Ordinal);
        Assert.Contains("unresolved", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("by-status/{status}", text, StringComparison.Ordinal);
        Assert.Contains("summary", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("acknowledge/{id:guid}", text, StringComparison.Ordinal);
        Assert.Contains("resolve/{id:guid}", text, StringComparison.Ordinal);
        Assert.Contains("ignore/{id:guid}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_mutations_are_post_only_without_delete()
    {
        var text = ReconciliationControllerSource();
        Assert.Contains("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_conflict_entity_has_resolution_status_fields()
    {
        var entity = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "SyncConflictRecord.cs"));
        var migration = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260518140000_AddSyncConflictResolutionStatus.cs");
        Assert.Contains("ResolutionStatus", entity, StringComparison.Ordinal);
        Assert.Contains("ResolvedBy", entity, StringComparison.Ordinal);
        Assert.True(File.Exists(migration));
    }

    [Fact]
    public void Reconciliation_workflow_declares_append_only_governance()
    {
        var text = ReconciliationControllerSource();
        Assert.Contains("GOVERNANCE / INTERNAL", text, StringComparison.Ordinal);
        Assert.Contains("no auto-healing", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no deletes", text, StringComparison.OrdinalIgnoreCase);
    }
}
