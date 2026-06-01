using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalDiagnosticsCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Cache_governance_documents_non_goals_and_restrictions()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalDiagnosticsCacheGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / NON-GOAL", text, StringComparison.Ordinal);
        Assert.Contains("in-process only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not durable", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not coherent across API instances", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NEVER contain", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Redis", text, StringComparison.Ordinal);
        Assert.Contains("distributed cache", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cache_ttl_constants_and_stale_thresholds_exist()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalDiagnosticsCacheConstants.cs"));
        Assert.Contains("ResilienceMetricsTtlSeconds = 30", text, StringComparison.Ordinal);
        Assert.Contains("ReconciliationSummaryTtlSeconds = 30", text, StringComparison.Ordinal);
        Assert.Contains("IncidentGroupsTtlSeconds = 45", text, StringComparison.Ordinal);
        Assert.Contains("ForensicSnapshotSummaryTtlSeconds = 15", text, StringComparison.Ordinal);
        Assert.Contains("AgingThresholdPercent = 0.5", text, StringComparison.Ordinal);
        Assert.Contains("NearExpiryThresholdPercent = 0.9", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_categories_are_stable()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalDiagnosticsCacheCategories.cs"));
        foreach (var category in new[]
                 {
                     "ResilienceMetrics", "ReconciliationSummary", "IncidentGroups", "IncidentSummary",
                     "AlertSignals", "AlertSummary", "ForensicSnapshotSummary"
                 })
            Assert.Contains(category, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Stale_risk_enum_values_exist()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalDiagnosticsCacheStaleRisk.cs"));
        foreach (var level in new[] { "Fresh", "Aging", "NearExpiry", "Expired" })
            Assert.Contains(level, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_implementation_has_no_persistence_or_forensic_dto_storage()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalDiagnosticsCacheService.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalForensicSnapshotDto", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_supports_try_get_envelope_for_readonly_peek()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "IOperationalDiagnosticsCache.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("TryGetEnvelope", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_observability_anchors_exist()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalDiagnosticsCacheService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational cache observability: cache hit", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache observability: cache miss", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache observability: cache bypass", text, StringComparison.Ordinal);
        Assert.Contains("Operational stale snapshot risk:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Solution_has_no_redis_or_distributed_cache_packages()
    {
        var csprojFiles = Directory.GetFiles(RepoRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        foreach (var csproj in csprojFiles)
        {
            var text = File.ReadAllText(csproj);
            Assert.DoesNotContain("StackExchange.Redis", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Microsoft.Extensions.Caching.StackExchangeRedis", text, StringComparison.OrdinalIgnoreCase);
        }

        var cacheSvc = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalDiagnosticsCacheService.cs"));
        Assert.DoesNotContain("IDistributedCache", cacheSvc, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_layer_has_no_hosted_or_background_services()
    {
        var infraServices = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services");
        foreach (var file in new[]
                 {
                     "OperationalDiagnosticsCacheService.cs",
                     "OperationalDiagnosticsCacheTelemetry.cs",
                     "OperationalDiagnosticsCacheDiagnosticsService.cs"
                 })
        {
            var text = File.ReadAllText(Path.Combine(infraServices, file));
            Assert.DoesNotContain("BackgroundService", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IHostedService", text, StringComparison.Ordinal);
        }
    }
}
