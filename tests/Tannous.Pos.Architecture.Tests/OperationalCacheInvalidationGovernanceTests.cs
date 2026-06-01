using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheInvalidationGovernanceTests
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
    public void Invalidation_governance_endpoints_are_get_only_without_payloads()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"invalidation-audit\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"freshness-recovery\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"invalidation-consistency\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"invalidation-pressure\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain(".Value", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_governance_has_no_persistence_or_background_services()
    {
        var infra = InfrastructureGlob();
        var projection = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalCacheInvalidationProjectionBuilder.cs");

        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_explainability_is_bounded()
    {
        var builder = Read("Tannous.Pos.Application", "Audit", "OperationalCacheInvalidationExplainabilityBuilder.cs");
        var composer = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceExplainabilityComposer.cs");
        var governance = Read("Tannous.Pos.Application", "Audit", "OperationalCacheInvalidationGovernance.cs");
        Assert.Contains("MaxReasonCodes", composer + governance, StringComparison.Ordinal);
        Assert.Contains("MaxReasonCodeLength", composer + governance, StringComparison.Ordinal);
        Assert.Contains("CrossCategoryCascade", builder, StringComparison.Ordinal);
        Assert.Contains("HighScopedInvalidationChurn", builder, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_drift_detector_does_not_throw()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalCacheInvalidationDriftDetector.cs");
        Assert.DoesNotContain("throw ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_audit_dto_has_no_envelope_or_payload_fields()
    {
        var dto = Read("Tannous.Pos.Application", "Audit", "OperationalCacheInvalidationAuditDto.cs");
        Assert.DoesNotContain("OperationalDiagnosticsCacheEnvelope", dto, StringComparison.Ordinal);
        Assert.DoesNotContain("Payload", dto, StringComparison.Ordinal);
        Assert.Contains("ReasonCodes", dto, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_telemetry_snapshot_exposes_step10_metrics()
    {
        var snapshot = Read(
            "Tannous.Pos.Application",
            "Audit",
            "OperationalDiagnosticsCacheTelemetrySnapshotDto.cs");
        Assert.Contains("CrossCategoryInvalidations", snapshot, StringComparison.Ordinal);
        Assert.Contains("FreshnessRecoveryCount", snapshot, StringComparison.Ordinal);
        Assert.Contains("InvalidationDriftCount", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalidation_diagnostics_service_logs_governance_prefixes()
    {
        var service = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();
        Assert.Contains("Operational invalidation governance:", service, StringComparison.Ordinal);
        Assert.Contains("Operational freshness recovery:", service, StringComparison.Ordinal);
        Assert.Contains("Operational invalidation drift:", service, StringComparison.Ordinal);
        Assert.Contains("Operational invalidation pressure:", service, StringComparison.Ordinal);
        Assert.Contains("Operational cache recovery guidance:", service, StringComparison.Ordinal);
    }
}
