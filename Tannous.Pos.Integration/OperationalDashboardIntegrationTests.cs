using System.Net.Http.Json;
using Tannous.Pos.Application.OperationalDashboard;

namespace Tannous.Pos.Integration;

public class OperationalDashboardIntegrationTests : IntegrationTestBase
{
    private const string DashboardBase = "/api/v1.0/internal/operational-audit/dashboard";

    public OperationalDashboardIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Dashboard_summary_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(DashboardBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalDashboardSummaryDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto!.Health);
        Assert.NotNull(dto.Risk);
        Assert.NotNull(dto.Pressure);
        Assert.NotNull(dto.Activity);
        Assert.False(string.IsNullOrWhiteSpace(dto.ReadinessSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.FingerprintStabilitySummary));
        Assert.True(dto.Recommendations.Count <= OperationalDashboardAggregation.MaxRecommendations);
        Assert.True(dto.ActiveConcerns.Count <= OperationalDashboardAggregation.MaxActiveConcerns);
    }

    [SkippableFact]
    public async Task Dashboard_recommendations_are_deterministically_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetDashboardAsync();
        var second = await GetDashboardAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Recommendations, second!.Recommendations);
        Assert.Equal(
            first.Recommendations.OrderBy(r => r, StringComparer.Ordinal).ToList(),
            first.Recommendations.ToList());
    }

    [SkippableFact]
    public async Task Dashboard_health_is_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetDashboardAsync();
        var second = await GetDashboardAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Health.State, second!.Health.State);
        Assert.Equal(first.Risk.Level, second.Risk.Level);
        Assert.Equal(first.Health.HealthFactors, second.Health.HealthFactors);
    }

    [SkippableFact]
    public async Task Dashboard_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var beforeReset = await GetDashboardAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var afterReset = await GetDashboardAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.Equal(beforeReset!.Health.State, afterReset!.Health.State);
        Assert.True(afterReset.Recommendations.Count <= OperationalDashboardAggregation.MaxRecommendations);
    }

    [SkippableFact]
    public async Task Dashboard_reuses_upstream_caches_without_stale_composition_after_invalidation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var warm = await GetDashboardAsync();
        ClearOperationalDiagnosticsUpstreamCaches();
        var afterClear = await GetDashboardAsync();

        Assert.NotNull(warm);
        Assert.NotNull(afterClear);
        Assert.Equal(warm!.Health.State, afterClear!.Health.State);
        Assert.True(afterClear.GeneratedAtUtc >= warm.GeneratedAtUtc);
    }

    private async Task<OperationalDashboardSummaryDto?> GetDashboardAsync()
    {
        var response = await _client.GetAsync(DashboardBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalDashboardSummaryDto>();
    }
}
