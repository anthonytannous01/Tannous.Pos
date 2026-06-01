using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalIncidentCorrelationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Incident_severity_and_types_are_stable()
    {
        var severity = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalIncidentSeverity.cs"));
        foreach (var level in new[] { "Low", "Moderate", "High", "Critical" })
            Assert.Contains(level, severity, StringComparison.Ordinal);

        var types = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalIncidentTypes.cs"));
        foreach (var type in new[]
                 {
                     "ReplayIncident", "ReconciliationIncident", "SettlementInconsistencyIncident",
                     "InventoryDriftIncident", "ResiliencePressureIncident", "ForensicSurvivabilityIncident",
                     "CascadingDegradationIncident"
                 })
            Assert.Contains(type, types, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_governance_documents_non_goals()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalIncidentGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("no PagerDuty", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no OpenTelemetry", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no automatic remediation", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Incident_endpoints_are_get_only_with_admin_authorization()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditIncidentsController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("internal/operational-audit/incidents", text, StringComparison.Ordinal);
        Assert.Contains("high-risk", text, StringComparison.Ordinal);
        Assert.Contains("cascading-degradation", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_correlation_service_groups_signals()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalIncidentCorrelationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GroupSignals", text, StringComparison.Ordinal);
        Assert.Contains("BuildCorrelationKey", text, StringComparison.Ordinal);
        Assert.Contains("OperationalIncidentRiskClassifier", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_observability_anchors_exist()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalIncidentCorrelationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational incident observability:", text, StringComparison.Ordinal);
        Assert.Contains("Operational causality visibility:", text, StringComparison.Ordinal);
        Assert.Contains("Operational correlation risk:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_includes_incident_correlation_enrichment()
    {
        var dto = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotDto.cs"));
        Assert.Contains("CorrelatedIncidentRisk", dto, StringComparison.Ordinal);
        Assert.Contains("CorrelatedSubsystems", dto, StringComparison.Ordinal);
        Assert.Contains("IncidentCorrelationSummary", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_risk_classifier_has_no_enforcement_logic()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalIncidentRiskClassifier.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ClassifySeverity", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Throttle", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Reject", text, StringComparison.Ordinal);
    }
}
