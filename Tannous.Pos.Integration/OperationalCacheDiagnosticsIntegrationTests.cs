using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalCacheDiagnosticsIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";

    public OperationalCacheDiagnosticsIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Cache_summary_returns_categories_and_ttl_metadata()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearOperationalDiagnosticsUpstreamCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsSummaryDto>();

        Assert.NotNull(summary);
        Assert.True(summary!.ActiveEntryCount >= 1);
        Assert.Contains(OperationalDiagnosticsCacheCategories.ResilienceMetrics, summary.CategoryTtlSeconds.Keys);
        Assert.Contains(OperationalDiagnosticsCacheCategories.ResilienceMetrics, summary.EntriesByCategory.Keys);
        Assert.All(summary.Entries, e => Assert.False(string.IsNullOrWhiteSpace(e.CacheKeyAlias)));
    }

    [SkippableFact]
    public async Task Cache_effectiveness_reflects_hits_after_repeated_upstream_calls()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearOperationalDiagnosticsUpstreamCaches();

        var telemetry = GetOperationalDiagnosticsCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ResilienceMetrics;
        var hitsBefore = GetOperationalCacheCategoryHits(telemetry, category);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var effectivenessResponse = await _client.GetAsync($"{CacheBase}/effectiveness");
        effectivenessResponse.EnsureSuccessStatusCode();
        var effectiveness =
            await effectivenessResponse.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsEffectivenessDto>();

        Assert.NotNull(effectiveness);
        Assert.True(GetOperationalCacheCategoryHits(telemetry, category) > hitsBefore);
        Assert.True(effectiveness!.TotalHits > 0);
        Assert.Contains(category, effectiveness.ByCategory.Keys);
    }

    [SkippableFact]
    public async Task Cache_stale_risk_endpoint_returns_structure_without_payloads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{CacheBase}/stale-risk");
        response.EnsureSuccessStatusCode();
        var staleRisk =
            await response.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto>();

        Assert.NotNull(staleRisk);
        Assert.True(staleRisk!.AgingEntryCount >= 0);
        Assert.True(staleRisk.NearExpiryEntryCount >= 0);
        Assert.True(staleRisk.ExpiredEntryCount >= 0);
    }

    [SkippableFact]
    public async Task Cache_pressure_endpoint_reports_bypass_counters()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{CacheBase}/pressure");
        response.EnsureSuccessStatusCode();
        var pressure =
            await response.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsPressureDto>();

        Assert.NotNull(pressure);
        Assert.True(pressure!.TotalBypasses >= 0);
        Assert.NotNull(pressure.BypassesByCategory);
    }

    [SkippableFact]
    public async Task Cashier_is_denied_cache_diagnostics_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{CacheBase}/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    protected override async Task SeedTestDataAsync(Tannous.Pos.Infrastructure.Data.PosDbContext context)
    {
        context.Users.Add(new Tannous.Pos.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = "cashier",
            NormalizedUsername = "CASHIER",
            Email = "cashier@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Tannous.Pos.Domain.Enums.Role.Cashier,
            FirstName = "Test",
            LastName = "Cashier",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

}
