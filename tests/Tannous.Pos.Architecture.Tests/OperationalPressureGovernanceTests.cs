using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalPressureGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static string InfrastructureGlob() =>
        string.Join(
            "\n",
            Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

    [Fact]
    public void Pressure_governance_endpoints_are_get_only_without_payloads()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"pressure-lifecycle\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"pressure-recovery\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"pressure-convergence\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("pressure-reset", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressure_reset_coordinator_does_not_touch_ef_or_replay()
    {
        var text = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsPressureResetCoordinator.cs");

        Assert.Contains("never mutates", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncConflict", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressure_governance_has_no_persistence_or_background_services()
    {
        var infra = InfrastructureGlob();
        var projection = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalPressureGovernanceProjectionBuilder.cs");

        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.GetTotalMemory", projection, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressure_governance_documented_as_non_authoritative()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalPressureGovernance.cs");
        Assert.Contains("no auto-healing", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no cross-instance", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("heuristic", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pressure_reset_coordinator_is_internal_only()
    {
        var webApi = Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "Tannous.Pos.WebApi"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText);

        var combined = string.Join("\n", webApi);
        Assert.DoesNotContain("IOperationalDiagnosticsPressureResetCoordinator", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressure_telemetry_snapshot_exposes_step12_metrics()
    {
        var snapshot = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalDiagnosticsCacheTelemetrySnapshotDto.cs");
        Assert.Contains("PressureRecoveryCycles", snapshot, StringComparison.Ordinal);
        Assert.Contains("StabilizationWindowResets", snapshot, StringComparison.Ordinal);
        Assert.Contains("AdaptiveTtlRecoveries", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressure_diagnostics_service_logs_governance_prefixes()
    {
        var service = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();
        Assert.Contains("Operational pressure lifecycle:", service, StringComparison.Ordinal);
        Assert.Contains("Operational pressure recovery:", service, StringComparison.Ordinal);
        Assert.Contains("Operational pressure convergence:", service, StringComparison.Ordinal);
    }
}
