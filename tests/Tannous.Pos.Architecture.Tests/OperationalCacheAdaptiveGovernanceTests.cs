using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheAdaptiveGovernanceTests
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
    public void Adaptive_ttl_classifier_never_exceeds_static_ttl_and_enforces_floors()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalCacheAdaptiveTtlClassifier.cs");
        Assert.Contains("if (adaptive > baseTtl)", text, StringComparison.Ordinal);
        Assert.Contains("ResilienceMinimumTtlSeconds = 5", text, StringComparison.Ordinal);
        Assert.Contains("StandardMinimumTtlSeconds = 10", text, StringComparison.Ordinal);
        Assert.Contains("0.5", text, StringComparison.Ordinal);
        Assert.Contains("0.25", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_infrastructure_has_no_background_warming_or_distributed_cache()
    {
        var infra = InfrastructureGlob();
        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Run(", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_dto_is_not_cached_in_cache_services()
    {
        var cacheServices = Read("Tannous.Pos.Infrastructure", "Services", "OperationalDiagnosticsCacheService.cs")
            + Read("Tannous.Pos.Infrastructure", "Services", "OperationalDiagnosticsCacheInvalidator.cs");
        Assert.DoesNotContain("OperationalForensicSnapshotDto", cacheServices, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<OperationalForensicSnapshotDto>", cacheServices, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_exposes_adaptive_get_endpoints_only()
    {
        var text = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        Assert.Contains("[HttpGet(\"adaptive-summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"warm-candidates\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"stability\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Adaptive_governance_logs_and_no_arbitrary_query_keys()
    {
        var diagnostics = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();
        var keyFactory = Read("Tannous.Pos.Application", "Audit", "OperationalDiagnosticsCacheKeyFactory.cs");

        Assert.Contains("Operational adaptive cache governance:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache warming visibility:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache stability:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational adaptive TTL reduction:", Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalCacheAdaptiveTtlHelper.cs"), StringComparison.Ordinal);
        Assert.Contains("no raw payloads, query strings", keyFactory, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpContext", keyFactory, StringComparison.Ordinal);
        Assert.DoesNotContain("Request.Query", keyFactory, StringComparison.Ordinal);
    }
}
