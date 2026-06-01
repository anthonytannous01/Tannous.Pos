using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalAlertCacheLayerIntegrationTests : IntegrationTestBase
{
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";

    public OperationalAlertCacheLayerIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Alert_summary_cache_hit_reuses_alert_layer()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        var telemetry = GetCacheTelemetry();
        var signalsCategory = OperationalDiagnosticsCacheCategories.AlertSignals;
        var summaryCategory = OperationalDiagnosticsCacheCategories.AlertSummary;
        var hitsBefore = GetCategoryHits(telemetry, signalsCategory) + GetCategoryHits(telemetry, summaryCategory);

        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();

        var hitsAfter = GetCategoryHits(telemetry, signalsCategory) + GetCategoryHits(telemetry, summaryCategory);
        Assert.True(hitsAfter > hitsBefore);
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

    private static long GetCategoryHits(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Hits : 0;
}
