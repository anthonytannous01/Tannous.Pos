using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

/// <summary>Isolated from hit/reuse tests so singleton pressure state does not force cache bypass on every case.</summary>
public class OperationalResilienceCacheBypassIntegrationTests : IntegrationTestBase
{
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";

    public OperationalResilienceCacheBypassIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Elevated_query_pressure_bypasses_resilience_metrics_cache()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ResilienceMetrics;
        var bypassBefore = telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats)
            ? stats.Bypasses
            : 0;

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        (await _client.GetAsync(
                $"{DiagnosticsBase}/conflicts/recent?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}&pageSize=9999"))
            .EnsureSuccessStatusCode();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var bypassAfter = telemetry.GetSnapshot().ByCategory.TryGetValue(category, out stats)
            ? stats.Bypasses
            : 0;
        Assert.True(bypassAfter > bypassBefore, "Query pressure should bypass resilience metrics cache.");
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }
}
