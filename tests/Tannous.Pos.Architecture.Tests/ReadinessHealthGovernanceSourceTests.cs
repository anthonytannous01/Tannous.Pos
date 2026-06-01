using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Readiness health check semantics (degraded vs unhealthy) without hosting the app.
/// </summary>
public class ReadinessHealthGovernanceSourceTests
{
    [Fact]
    public void DatabaseHealthCheck_reports_degraded_for_pending_migrations_or_missing_prereqs()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "HealthChecks", "DatabaseHealthCheck.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("HealthCheckResult.Degraded", text, StringComparison.Ordinal);
        Assert.Contains("pending migrations", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HealthCheckResult.Healthy", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseHealthCheck_unhealthy_path_exists_for_connection_failures()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "HealthChecks", "DatabaseHealthCheck.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("HealthCheckResult.Unhealthy", text, StringComparison.Ordinal);
    }
}
