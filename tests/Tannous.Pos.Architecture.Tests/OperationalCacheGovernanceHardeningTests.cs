using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheGovernanceHardeningTests
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

    private static string ApplicationAuditGlob() =>
        string.Join(
            "\n",
            Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit"),
                    "OperationalCache*.cs",
                    SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText));

    [Fact]
    public void Governance_overview_endpoint_is_get_only_without_payload_exposure()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var diagnostics = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");

        Assert.Contains("[HttpGet(\"governance-overview\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.Contains("GetGovernanceOverviewAsync", diagnostics, StringComparison.Ordinal);
        Assert.Contains("OperationalCacheGovernanceOverviewDto", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain(".Value", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalDiagnosticsCacheEnvelope<", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_governance_has_no_hosted_services_redis_or_machine_memory_inspection()
    {
        var infra = InfrastructureGlob();

        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Run(", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.GetTotalMemory(", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.WorkingSet", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Adaptive_ttl_floors_and_caps_preserved_under_pressure_adjustment()
    {
        var classifier = Read("Tannous.Pos.Application", "Audit", "OperationalCacheAdaptiveTtlClassifier.cs");
        Assert.Contains("ApplyCachePressureSeverity", classifier, StringComparison.Ordinal);
        Assert.Contains("GetMinimumTtlSeconds", classifier, StringComparison.Ordinal);
        Assert.Contains("if (adaptive > baseTtl)", classifier, StringComparison.Ordinal);
        Assert.Contains("ResilienceMinimumTtlSeconds = 5", classifier, StringComparison.Ordinal);
        Assert.Contains("StandardMinimumTtlSeconds = 10", classifier, StringComparison.Ordinal);
    }

    [Fact]
    public void Governance_structured_log_anchors_exist()
    {
        var diagnostics = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();

        Assert.Contains("Operational cache governance overview:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache cardinality:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache degradation:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache survivability:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Operational cache pressure classification:", diagnostics, StringComparison.Ordinal);
    }

    [Fact]
    public void Forensic_snapshot_dto_is_not_cached_in_governance_paths()
    {
        var text = InfrastructureGlob();
        Assert.DoesNotContain("GetOrCreateAsync<OperationalForensicSnapshotDto>", text, StringComparison.Ordinal);
    }
}
