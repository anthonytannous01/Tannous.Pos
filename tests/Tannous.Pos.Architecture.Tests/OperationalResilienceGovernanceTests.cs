using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalResilienceGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Resilience_constants_and_degraded_mode_types_exist()
    {
        var constants = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalResilienceConstants.cs"));
        var modes = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalDegradedModeTypes.cs"));
        Assert.Contains("ReplayStormReceiptCountThreshold", constants, StringComparison.Ordinal);
        foreach (var mode in new[] { "Normal", "ElevatedQueryPressure", "ReconciliationPressure", "ExportPressure", "AuditPersistencePressure", "ReplayStormRisk" })
            Assert.Contains(mode, modes, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_governance_documents_non_goals()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalResilienceGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("no distributed circuit breaker", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no external queueing", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("best-effort", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resilience_endpoints_are_get_only_with_admin_authorization()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditResilienceController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("internal/operational-audit/resilience", text, StringComparison.Ordinal);
        Assert.Contains("summary", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("degraded-modes", text, StringComparison.Ordinal);
        Assert.Contains("pressure-indicators", text, StringComparison.Ordinal);
        Assert.Contains("replay-risk-summary", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_observability_anchors_exist()
    {
        var recorder = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditRecorder.cs"));
        var resilience = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalResilienceDiagnosticsService.cs"));
        var forensic = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs"));
        Assert.Contains("Operational resilience observability:", recorder, StringComparison.Ordinal);
        Assert.Contains("Operational degraded mode:", recorder, StringComparison.Ordinal);
        Assert.Contains("Operational resilience observability:", resilience, StringComparison.Ordinal);
        Assert.Contains("Operational backpressure visibility:", forensic, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_includes_export_pressure_metadata()
    {
        var dto = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalForensicSnapshotDto.cs"));
        Assert.Contains("ExportPressureClassification", dto, StringComparison.Ordinal);
        Assert.Contains("TruncationSeverity", dto, StringComparison.Ordinal);
        Assert.Contains("ExportSurvivabilityWarning", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Audit_recorder_remains_best_effort_without_retries()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditRecorder.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("best-effort", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Retry", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAuditPersistenceTelemetry", text, StringComparison.Ordinal);
    }
}
