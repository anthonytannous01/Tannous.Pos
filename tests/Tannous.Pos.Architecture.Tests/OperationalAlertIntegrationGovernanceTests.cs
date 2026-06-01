using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAlertIntegrationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Alert_diagnostics_controller_route_is_stable()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAlertDiagnosticsController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"current\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"critical\")]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Alert_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalAlertSignalService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalAlertSignalService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Integration_tests_cover_alert_diagnostics()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "OperationalAlertDiagnosticsIntegrationTests.cs");
        Assert.True(File.Exists(path), "Missing OperationalAlertDiagnosticsIntegrationTests.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("operational-audit/alerts", text, StringComparison.Ordinal);
    }
}
