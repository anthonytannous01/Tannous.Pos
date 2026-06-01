using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;

namespace Tannous.Pos.Integration;

public class OperationalTriageIntegrationTests : IntegrationTestBase
{
    private const string TriageBase = "/api/v1.0/internal/operational-audit/triage";
    private const string TriageRecommendationsBase = "/api/v1.0/internal/operational-audit/triage/recommendations";

    public OperationalTriageIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Triage_queue_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(TriageBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalTriageQueueDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.TriageNote));
        Assert.True(dto.Items.Count <= OperationalTriageAggregation.MaxTriageItems);
        Assert.True(dto.AttentionItems.Count <= OperationalTriageAggregation.MaxAttentionItems);
        Assert.True(dto.Correlations.Count <= OperationalTriageAggregation.MaxCorrelations);
    }

    [SkippableFact]
    public async Task Triage_recommendations_are_bounded_and_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var recommendations = await GetRecommendationsAsync();
        Assert.NotNull(recommendations);
        Assert.True(recommendations!.Count <= OperationalTriageAggregation.MaxRecommendations);
        Assert.Equal(
            recommendations.OrderBy(r => r.Priority).ThenBy(r => r.Title, StringComparer.Ordinal).ToList(),
            recommendations.ToList());
    }

    [SkippableFact]
    public async Task Triage_queue_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetTriageQueueAsync();
        var second = await GetTriageQueueAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.OverallPriority, second!.OverallPriority);
        Assert.Equal(first.Items.Select(i => i.Category), second.Items.Select(i => i.Category));
    }

    [SkippableFact]
    public async Task Triage_routes_use_existing_operational_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var queue = await GetTriageQueueAsync();
        Assert.NotNull(queue);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalTriageAggregation.RouteDashboard,
            OperationalTriageAggregation.RouteReconciliationWorkbench,
            OperationalTriageAggregation.RouteInventoryWorkbench,
            OperationalTriageAggregation.RouteReplayWorkbench,
            OperationalTriageAggregation.RouteTrendSummary,
            OperationalTriageAggregation.RouteTimeline
        };

        Assert.All(queue!.Items, item => Assert.Contains(item.RecommendedRoute, allowed));
    }

    [SkippableFact]
    public async Task Triage_items_have_investigation_reason_and_correlated_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var queue = await GetTriageQueueAsync();
        Assert.NotNull(queue);
        Assert.NotEmpty(queue!.Items);
        Assert.All(queue.Items, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Summary));
            Assert.False(string.IsNullOrWhiteSpace(item.InvestigationReason));
            Assert.False(string.IsNullOrWhiteSpace(item.SuggestedOperatorAction));
        });
    }

    [SkippableFact]
    public async Task Triage_correlations_generated_from_queue()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var queue = await GetTriageQueueAsync();
        Assert.NotNull(queue);
        Assert.NotEmpty(queue!.Correlations);
    }

    [SkippableFact]
    public async Task Triage_reset_clears_dependent_stores_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TriageBase);
        ResetOperationalStores();

        using var scope = _factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().GetEvents());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().GetSnapshots());

        var queue = await GetTriageQueueAsync();
        Assert.NotNull(queue);
        Assert.NotEmpty(queue!.Items);
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
    }

    private async Task<OperationalTriageQueueDto?> GetTriageQueueAsync()
    {
        var response = await _client.GetAsync(TriageBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalTriageQueueDto>();
    }

    private async Task<IReadOnlyList<OperationalTriageRecommendationDto>?> GetRecommendationsAsync()
    {
        var response = await _client.GetAsync(TriageRecommendationsBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalTriageRecommendationDto>>();
    }
}
