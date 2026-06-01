using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalCacheGovernanceFinalizationIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";

    public OperationalCacheGovernanceFinalizationIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Governance_audit_aggregates_projections()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-audit");
        response.EnsureSuccessStatusCode();
        var audit = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceAuditDto>();

        Assert.NotNull(audit);
        Assert.NotEmpty(audit!.DominantTtlMode);
        Assert.NotNull(audit.Drift);
        Assert.NotNull(audit.Consistency);
        Assert.NotEmpty(audit.Recommendations);
    }

    [SkippableFact]
    public async Task Drift_detection_visible_in_governance_audit()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        using (var scope = _factory.Services.CreateScope())
        {
            var telemetry = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
            for (var i = 0; i < 25; i++)
                telemetry.RecordBypass(OperationalDiagnosticsCacheCategories.ResilienceMetrics);
        }

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var audit = await GetGovernanceAuditAsync();
        Assert.NotNull(audit?.Drift);
    }

    [SkippableFact]
    public async Task Governance_consistency_endpoint_returns_advisory_result()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/governance-consistency");
        response.EnsureSuccessStatusCode();
        var consistency = await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceConsistencyDto>();

        Assert.NotNull(consistency);
        Assert.NotNull(consistency!.ConsistencyNotes);
        Assert.NotNull(consistency.InconsistencySignals);
    }

    [SkippableFact]
    public async Task Survivability_scoring_returns_bounded_classification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/survivability");
        response.EnsureSuccessStatusCode();
        var survivability = await response.Content.ReadFromJsonAsync<OperationalCacheSurvivabilityDto>();

        Assert.NotNull(survivability);
        Assert.InRange(survivability!.SurvivabilityScore, 0, 100);
        Assert.Contains(
            survivability.ClassificationLabel,
            new[] { "Durable", "Stable", "Fragile", "Volatile" });
    }

    [SkippableFact]
    public async Task Recommendations_generated_in_governance_audit()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var audit = await GetGovernanceAuditAsync();
        Assert.NotNull(audit);
        Assert.NotEmpty(audit!.Recommendations);
        Assert.All(audit.Recommendations, r => Assert.False(string.IsNullOrWhiteSpace(r.Code)));
    }

    [SkippableFact]
    public async Task Explainability_fields_populated_on_stability()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/stability");
        response.EnsureSuccessStatusCode();
        var stability = await response.Content.ReadFromJsonAsync<OperationalCacheStabilityDto>();

        Assert.NotNull(stability);
        Assert.NotEmpty(stability!.ReasonCodes);
        Assert.NotEmpty(stability.RecommendedActions);
    }

    [SkippableFact]
    public async Task Alert_cache_current_endpoint_unchanged()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{AlertsBase}/current")).EnsureSuccessStatusCode();
    }

    private async Task<OperationalCacheGovernanceAuditDto?> GetGovernanceAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceAuditDto>();
    }
}
