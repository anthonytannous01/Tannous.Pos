using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalAlertCacheIntegrationTests : IntegrationTestBase
{
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";

    public OperationalAlertCacheIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Repeated_alert_summary_reuses_upstream_cache_categories()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        var telemetry = GetOperationalDiagnosticsCacheTelemetry();
        var resilienceHitsBefore = GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ResilienceMetrics);
        var reconciliationHitsBefore = GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ReconciliationSummary);
        var incidentHitsBefore = GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.IncidentGroups);

        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();
        ClearOperationalAlertLayerCachesOnly();

        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();

        Assert.True(
            GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ResilienceMetrics) > resilienceHitsBefore);
        Assert.True(
            GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ReconciliationSummary) > reconciliationHitsBefore);
        Assert.True(
            GetOperationalCacheCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.IncidentGroups) > incidentHitsBefore);
    }
}
