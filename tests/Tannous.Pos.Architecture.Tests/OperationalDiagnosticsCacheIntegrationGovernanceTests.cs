using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalDiagnosticsCacheIntegrationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Program_registers_memory_cache_and_cache_services()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("AddMemoryCache", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalDiagnosticsCacheTelemetry", program, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheTelemetry", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalDiagnosticsCache", program, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalDiagnosticsCacheDiagnosticsService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheDiagnosticsService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_controller_is_registered_for_step_five()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        Assert.True(File.Exists(path), "OperationalAuditCacheDiagnosticsController must exist for Step 5.");
    }

    [Fact]
    public void Operational_audit_cache_routes_exist()
    {
        var controller = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs"));
        Assert.Contains("internal/operational-audit/cache", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_references_memory_cache_package()
    {
        var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Tannous.Pos.Infrastructure.csproj"));
        Assert.Contains("Microsoft.Extensions.Caching.Memory", csproj, StringComparison.Ordinal);
    }
}
