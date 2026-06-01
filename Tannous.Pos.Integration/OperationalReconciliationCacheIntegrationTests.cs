using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Integration;

public class OperationalReconciliationCacheIntegrationTests : IntegrationTestBase
{
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";

    public OperationalReconciliationCacheIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Reconciliation_summary_cache_miss_then_hit()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearReconciliationSummaryCache();

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ReconciliationSummary;
        var hitsBefore = GetCategoryHits(telemetry, category);
        var missesBefore = GetCategoryMisses(telemetry, category);

        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryMisses(telemetry, category) > missesBefore, "First call should register a cache miss.");
        Assert.True(GetCategoryHits(telemetry, category) > hitsBefore, "Second call should register a cache hit.");
    }

    [SkippableFact]
    public async Task Reconciliation_summary_is_stable_across_cached_calls()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearReconciliationSummaryCache();

        var first = await _client.GetAsync($"{ReconciliationBase}/summary");
        first.EnsureSuccessStatusCode();
        var firstDto = await first.Content.ReadFromJsonAsync<ReconciliationSummaryDto>();

        var second = await _client.GetAsync($"{ReconciliationBase}/summary");
        second.EnsureSuccessStatusCode();
        var secondDto = await second.Content.ReadFromJsonAsync<ReconciliationSummaryDto>();

        Assert.NotNull(firstDto);
        Assert.NotNull(secondDto);
        Assert.Equal(firstDto!.UnresolvedCount, secondDto!.UnresolvedCount);
        Assert.Equal(firstDto.ReplayMismatchCount, secondDto.ReplayMismatchCount);
    }

    private void ClearReconciliationSummaryCache()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>();
        cache.Remove(
            OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
            OperationalDiagnosticsCacheCategories.ReconciliationSummary);
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
