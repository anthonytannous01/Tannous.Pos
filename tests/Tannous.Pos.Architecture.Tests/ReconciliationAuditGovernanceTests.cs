using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ReconciliationAuditGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Reconciliation_workflow_audit_category_and_actions_exist()
    {
        var categories = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditCategories.cs"));
        var actions = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditReconciliationActions.cs"));
        Assert.Contains("ReconciliationWorkflow", categories, StringComparison.Ordinal);
        foreach (var action in new[] { "ConflictAcknowledged", "ConflictResolved", "ConflictIgnored", "InvestigationStarted" })
            Assert.Contains(action, actions, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_service_records_workflow_audit_on_transition()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OperationalAuditCategories.ReconciliationWorkflow", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAuditRecorder", text, StringComparison.Ordinal);
        Assert.Contains("previousStatus", text, StringComparison.Ordinal);
        Assert.Contains("newStatus", text, StringComparison.Ordinal);
        Assert.Contains("conflictType", text, StringComparison.Ordinal);
        Assert.Contains("actor", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_observability_anchors_exist()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational reconciliation observability: reconciliation status changed", text, StringComparison.Ordinal);
        Assert.Contains("Operational reconciliation observability: unresolved conflict query executed", text, StringComparison.Ordinal);
        Assert.Contains("Operational reconciliation observability: reconciliation summary query executed", text, StringComparison.Ordinal);
        Assert.Contains("Operational reconciliation observability: reconciliation audit persisted", text, StringComparison.Ordinal);
        Assert.Contains("Operational reconciliation observability: reconciliation notes truncated", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_summary_dto_includes_required_counts()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "ReconciliationSummaryDto.cs");
        var text = File.ReadAllText(path);
        foreach (var field in new[]
                 {
                     "UnresolvedCount",
                     "InvestigatingCount",
                     "ResolvedCount",
                     "ReplayMismatchCount",
                     "ConcurrencyConflictCount",
                     "LifecycleConflictCount",
                     "InventoryDriftRiskCount"
                 })
        {
            Assert.Contains(field, text, StringComparison.Ordinal);
        }
    }
}
