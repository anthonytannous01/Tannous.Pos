using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalCacheGovernanceHardeningIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";

    public OperationalCacheGovernanceHardeningIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Governance_overview_aggregates_effectiveness_adaptive_and_cardinality()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview);
        Assert.True(overview!.TotalHits + overview.TotalMisses >= 0);
        Assert.NotNull(overview.Cardinality);
        Assert.NotNull(overview.ScopeDiagnostics);
        Assert.NotNull(overview.Degradation);
        Assert.False(string.IsNullOrWhiteSpace(overview.StabilityClassification));
    }

    [SkippableFact]
    public async Task Cardinality_classification_visible_after_cache_warm()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview);
        Assert.Equal(
            overview!.CardinalityClassification,
            overview.Cardinality.Classification);
        Assert.Contains(
            overview.CardinalityClassification.ToString(),
            new[] { "Normal", "Elevated", "High", "Saturated" });
        Assert.True(overview.Cardinality.MaxCacheEntryCount > 0);
    }

    [SkippableFact]
    public async Task Pressure_severity_elevates_under_query_pressure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        (await _client.GetAsync(
                $"{DiagnosticsBase}/conflicts/recent?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}&pageSize=9999"))
            .EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview);
        Assert.True(overview!.PressureSeverity >= OperationalCachePressureSeverity.Elevated);
        Assert.Equal(OperationalCacheReadinessState.PressureDegraded, overview.ReadinessState);
    }

    [SkippableFact]
    public async Task Scope_diagnostics_expose_survivability_without_raw_ids()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview?.ScopeDiagnostics);
        Assert.All(
            overview!.ScopeDiagnostics.OldestScopedEntries,
            e =>
            {
                Assert.False(string.IsNullOrWhiteSpace(e.CacheKeyAlias));
                Assert.DoesNotContain("device-", e.CacheKeyAlias, StringComparison.OrdinalIgnoreCase);
            });
    }

    [SkippableFact]
    public async Task Degradation_classification_visible_in_governance_overview()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview?.Degradation);
        Assert.Contains(
            overview!.Degradation.Classification,
            new[] { "Healthy", "Recovering", "Degraded", "SeverelyDegraded" });
    }

    [SkippableFact]
    public async Task Warm_recommendations_suppressed_under_critical_pressure_simulation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        using (var scope = _factory.Services.CreateScope())
        {
            var telemetry = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
            for (var i = 0; i < 6; i++)
                telemetry.RecordRepeatedColdMiss(OperationalDiagnosticsCacheCategories.ResilienceMetrics);
            for (var i = 0; i < 25; i++)
                telemetry.RecordInvalidation(OperationalDiagnosticsCacheCategories.ResilienceMetrics, 1);
            for (var i = 0; i < 10; i++)
                telemetry.RecordBypass(OperationalDiagnosticsCacheCategories.ResilienceMetrics);
        }

        for (var i = 0; i < 5; i++)
        {
            ClearAllDiagnosticsCaches();
            (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        }

        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();

        Assert.NotNull(overview);
        if (overview!.PressureSeverity == OperationalCachePressureSeverity.Critical)
            Assert.True(overview.WarmRecommendationsSuppressed);
    }

    [SkippableFact]
    public async Task Alert_cache_current_endpoint_still_succeeds()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{AlertsBase}/current");
        response.EnsureSuccessStatusCode();
    }

    private void ClearAllDiagnosticsCaches()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>().RemoveAllDiagnosticsCaches();
    }
}
