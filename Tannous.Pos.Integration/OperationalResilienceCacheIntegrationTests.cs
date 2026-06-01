using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalResilienceCacheIntegrationTests : IntegrationTestBase
{
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";

    public OperationalResilienceCacheIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Resilience_summary_cache_miss_then_hit_on_second_call()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ResilienceMetrics;
        var hitsBefore = GetCategoryHits(telemetry, category);
        var missesBefore = GetCategoryMisses(telemetry, category);

        var first = await _client.GetAsync($"{ResilienceBase}/summary");
        first.EnsureSuccessStatusCode();

        var second = await _client.GetAsync($"{ResilienceBase}/summary");
        second.EnsureSuccessStatusCode();

        Assert.True(GetCategoryMisses(telemetry, category) > missesBefore, "First call should register a cache miss.");
        Assert.True(GetCategoryHits(telemetry, category) > hitsBefore, "Second call should register a cache hit.");
    }

    [SkippableFact]
    public async Task Resilience_endpoints_reuse_cached_metrics_snapshot()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ResilienceMetrics;
        var hitsBefore = GetCategoryHits(telemetry, category);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/degraded-modes")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/pressure-indicators")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/replay-risk-summary")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryHits(telemetry, category) - hitsBefore >= 3, "Subsequent resilience endpoints should reuse cached metrics.");
    }

    [SkippableFact]
    public void Stale_risk_classifier_marks_aging_without_flaky_timing()
    {
        var created = DateTime.UtcNow.AddSeconds(-20);
        var expires = created.AddSeconds(30);
        var now = created.AddSeconds(18);

        var risk = OperationalDiagnosticsCacheStaleRiskClassifier.Classify(created, expires, now);
        Assert.Equal(OperationalDiagnosticsCacheStaleRisk.Aging, risk);
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }

    private static long GetCategoryHits(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Hits : 0;

    private static long GetCategoryMisses(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Misses : 0;
}
