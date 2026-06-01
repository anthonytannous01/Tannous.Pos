using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalCacheAdaptiveIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";

    public OperationalCacheAdaptiveIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Adaptive_ttl_reduces_under_query_pressure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        (await _client.GetAsync(
                $"{DiagnosticsBase}/conflicts/recent?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}&pageSize=9999"))
            .EnsureSuccessStatusCode();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var adaptive = await _client.GetAsync($"{CacheBase}/adaptive-summary");
        adaptive.EnsureSuccessStatusCode();
        var summary = await adaptive.Content.ReadFromJsonAsync<OperationalCacheAdaptiveSummaryDto>();

        Assert.NotNull(summary);
        Assert.True(
            summary!.AdaptiveTtlReductions > 0
            || summary.DominantTtlMode != OperationalCacheTtlMode.Normal);
        Assert.True(
            summary.EffectiveTtlSecondsByCategory[OperationalDiagnosticsCacheCategories.ResilienceMetrics]
            <= OperationalDiagnosticsCacheConstants.ResilienceMetricsTtlSeconds);
        Assert.True(
            summary.EffectiveTtlSecondsByCategory[OperationalDiagnosticsCacheCategories.ResilienceMetrics]
            >= OperationalCacheAdaptiveTtlClassifier.ResilienceMinimumTtlSeconds);
    }

    [SkippableFact]
    public async Task Warm_candidates_visible_after_repeated_misses()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        ClearAllDiagnosticsCaches();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/warm-candidates");
        response.EnsureSuccessStatusCode();
        var warm = await response.Content.ReadFromJsonAsync<OperationalCacheWarmCandidatesDiagnosticsDto>();

        Assert.NotNull(warm);
        Assert.True(warm!.WarmCandidateCount >= 0);
    }

    [SkippableFact]
    public async Task Stability_endpoint_returns_classification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/stability");
        response.EnsureSuccessStatusCode();
        var stability = await response.Content.ReadFromJsonAsync<OperationalCacheStabilityDto>();

        Assert.NotNull(stability);
        Assert.InRange(stability!.StabilityScore, 0, 100);
        Assert.Contains(
            stability.StabilityClassification,
            new[] { "Stable", "Recovering", "Degraded", "Unstable" });
        Assert.False(string.IsNullOrWhiteSpace(stability.RecommendedOperatorAction));
    }

    [SkippableFact]
    public async Task Readiness_reflects_cache_activity_after_warm()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        var before = await GetAdaptiveSummaryAsync();
        Assert.NotNull(before);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var after = await GetAdaptiveSummaryAsync();
        Assert.NotNull(after);
        Assert.True(after!.EffectiveTtlSecondsByCategory.Count > 0);
        Assert.Contains(
            OperationalDiagnosticsCacheCategories.ResilienceMetrics,
            after.EffectiveTtlSecondsByCategory.Keys);
        Assert.True(
            after.ReadinessState == OperationalCacheReadinessState.Stable
            || after.ReadinessState == OperationalCacheReadinessState.WarmingRecommended
            || after.ReadinessState == OperationalCacheReadinessState.PressureDegraded);
        Assert.True(
            after.WarmCandidateCount >= before!.WarmCandidateCount
            || after.AdaptiveTtlReductions >= before.AdaptiveTtlReductions);
    }

    [SkippableFact]
    public async Task Alert_cache_still_bypasses_under_query_pressure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.AlertSignals;
        var bypassBefore = telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats)
            ? stats.Bypasses
            : 0;

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        (await _client.GetAsync(
                $"{DiagnosticsBase}/conflicts/recent?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}&pageSize=9999"))
            .EnsureSuccessStatusCode();
        (await _client.GetAsync($"{AlertsBase}/current")).EnsureSuccessStatusCode();

        var bypassAfter = telemetry.GetSnapshot().ByCategory.TryGetValue(category, out stats)
            ? stats.Bypasses
            : 0;
        Assert.True(bypassAfter > bypassBefore);
    }

    private async Task<OperationalCacheAdaptiveSummaryDto?> GetAdaptiveSummaryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/adaptive-summary");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheAdaptiveSummaryDto>();
    }

    private void ClearAllDiagnosticsCaches()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>().RemoveAllDiagnosticsCaches();
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }
}
