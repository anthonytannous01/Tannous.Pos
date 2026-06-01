using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheDiagnosticsControllerGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string ControllerSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs"));

    [Fact]
    public void Cache_diagnostics_controller_remains_get_only_admin_internal()
    {
        var text = ControllerSource();
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("internal/operational-audit/cache", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }
}
