using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAlertGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Alert_severity_and_types_are_stable()
    {
        var severity = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAlertSeverity.cs"));
        foreach (var level in new[] { "Info", "Warning", "Critical" })
            Assert.Contains(level, severity, StringComparison.Ordinal);

        var types = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAlertTypes.cs"));
        foreach (var type in new[]
                 {
                     "ReplayStormRisk", "AuditPersistencePressure", "InventoryDriftEscalation",
                     "CascadingOperationalPressure", "ReconciliationBacklog", "ConflictEscalation",
                     "ExportTruncationPressure", "LifecycleConflictSpike"
                 })
            Assert.Contains(type, types, StringComparison.Ordinal);
    }

    [Fact]
    public void Alert_governance_documents_non_goals_and_no_persistence()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAlertGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / NON-GOAL", text, StringComparison.Ordinal);
        Assert.Contains("NOT persisted", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT delivered externally", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no paging/on-call", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no automatic remediation", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Alert_service_has_no_persistence_writes()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAlertSignalService.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_dbContext.Add", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_context.Add", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Alert_diagnostics_endpoints_are_get_only_with_admin_authorization()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAlertDiagnosticsController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("internal/operational-audit/alerts", text, StringComparison.Ordinal);
        Assert.Contains("replay-pressure", text, StringComparison.Ordinal);
        Assert.Contains("inventory-risk", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Alert_observability_anchors_exist()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAlertSignalService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational alert visibility:", text, StringComparison.Ordinal);
        Assert.Contains("Operational escalation visibility:", text, StringComparison.Ordinal);
        Assert.Contains("Operational pressure escalation:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_includes_additive_alert_fields()
    {
        var dto = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotDto.cs"));
        Assert.Contains("AlertSignals", dto, StringComparison.Ordinal);
        Assert.Contains("AlertSummary", dto, StringComparison.Ordinal);
        Assert.Contains("EscalationRisk", dto, StringComparison.Ordinal);
        Assert.Contains("OperationalPressureSummary", dto, StringComparison.Ordinal);
    }
}
