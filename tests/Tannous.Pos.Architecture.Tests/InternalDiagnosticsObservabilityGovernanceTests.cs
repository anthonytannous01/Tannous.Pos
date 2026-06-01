using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class InternalDiagnosticsObservabilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Operational_audit_diagnostics_observability_anchors_exist()
    {
        var queryPath = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditQueryService.cs");
        var controllerPath = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditDiagnosticsController.cs");

        var queryText = File.ReadAllText(queryPath);
        var controllerText = File.ReadAllText(controllerPath);

        Assert.Contains("Operational audit diagnostics: timeline query executed", queryText, StringComparison.Ordinal);
        Assert.Contains("Operational audit diagnostics: conflict query executed", queryText, StringComparison.Ordinal);
        Assert.True(
            queryText.Contains("Operational audit diagnostics: pagination limit enforced", StringComparison.Ordinal)
            || queryText.Contains("Operational query protection: pagination clamped", StringComparison.Ordinal),
            "Expected pagination protection observability anchor in OperationalAuditQueryService.");
        Assert.Contains("Operational audit diagnostics: diagnostics authorization path", controllerText, StringComparison.Ordinal);
    }
}
