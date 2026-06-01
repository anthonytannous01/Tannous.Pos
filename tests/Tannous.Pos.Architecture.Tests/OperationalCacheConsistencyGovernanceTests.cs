using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheConsistencyGovernanceTests
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
    public void Consistency_governance_endpoints_are_get_only_without_payloads()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"consistency-recovery\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"containment-audit\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"propagation-diagnostics\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"consistency-confidence\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain(".Value", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_governance_has_no_persistence_or_background_services()
    {
        var infra = InfrastructureGlob();
        var projection = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalCacheConsistencyProjectionBuilder.cs");

        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_explainability_is_bounded()
    {
        var builder = Read("Tannous.Pos.Application", "Audit", "OperationalCacheConsistencyExplainabilityBuilder.cs");
        var composer = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceExplainabilityComposer.cs");
        var governance = Read("Tannous.Pos.Application", "Audit", "OperationalCacheConsistencyGovernance.cs");
        Assert.Contains("MaxExplainabilityItems", composer + governance, StringComparison.Ordinal);
        Assert.Contains("RecoveryWindowExtended", builder, StringComparison.Ordinal);
        Assert.Contains("PropagationEscalated", builder, StringComparison.Ordinal);
    }

    [Fact]
    public void Propagation_detector_does_not_throw()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalCachePropagationDetector.cs");
        Assert.DoesNotContain("throw ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_dtos_have_no_envelope_or_payload_fields()
    {
        var dto = Read("Tannous.Pos.Application", "Audit", "OperationalCacheContainmentAuditDto.cs");
        Assert.DoesNotContain("OperationalDiagnosticsCacheEnvelope", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("Payload", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_telemetry_snapshot_exposes_step11_metrics()
    {
        var snapshot = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalDiagnosticsCacheTelemetrySnapshotDto.cs");
        Assert.Contains("ConsistencyRecoveryCycles", snapshot, StringComparison.Ordinal);
        Assert.Contains("PropagationDetections", snapshot, StringComparison.Ordinal);
        Assert.Contains("ConsistencyConfidenceDrops", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_diagnostics_service_logs_governance_prefixes()
    {
        var service = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();
        Assert.Contains("Operational consistency recovery:", service, StringComparison.Ordinal);
        Assert.Contains("Operational containment governance:", service, StringComparison.Ordinal);
        Assert.Contains("Operational propagation visibility:", service, StringComparison.Ordinal);
        Assert.Contains("Operational consistency confidence:", service, StringComparison.Ordinal);
        Assert.Contains("Operational recovery stabilization:", service, StringComparison.Ordinal);
    }
}
