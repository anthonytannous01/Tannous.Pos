using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalNavigationIntegrationTests : IntegrationTestBase
{
    private const string NavigationBase = "/api/v1.0/internal/operational-audit/navigation";
    private const string NavigationRoutesBase = "/api/v1.0/internal/operational-audit/navigation/routes";
    private const string ReplayWorkbenchBase = "/api/v1.0/internal/operational-audit/replay-workbench/pressure";

    public OperationalNavigationIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Navigation_index_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(NavigationBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalNavigationIndexDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.NavigationNote));
        Assert.Equal(6, dto.Sections.Count);
        Assert.True(dto.Recommendations.Count <= OperationalNavigationAggregation.MaxRecommendations);
        Assert.True(dto.AttentionItems.Count <= OperationalNavigationAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Navigation_routes_use_existing_operational_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(NavigationRoutesBase);
        response.EnsureSuccessStatusCode();

        var routes = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalNavigationRouteDto>>();
        Assert.NotNull(routes);
        Assert.NotEmpty(routes!);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalNavigationAggregation.RouteDashboard,
            OperationalNavigationAggregation.RouteReconciliationWorkbench,
            OperationalNavigationAggregation.RouteInventoryWorkbench,
            OperationalNavigationAggregation.RouteReplayWorkbench,
            OperationalNavigationAggregation.RouteTrendSummary
        };

        Assert.All(routes!, route => Assert.Contains(route.RelativeRoute, allowed));
    }

    [SkippableFact]
    public async Task Navigation_index_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetNavigationIndexAsync();
        var second = await GetNavigationIndexAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.OverallSeverity, second!.OverallSeverity);
        Assert.Equal(first.OverallState, second.OverallState);
        Assert.Equal(first.Sections.Select(s => s.SectionName), second.Sections.Select(s => s.SectionName));
    }

    [SkippableFact]
    public async Task Navigation_recommendations_are_deterministically_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var index = await GetNavigationIndexAsync();
        Assert.NotNull(index);
        Assert.Equal(
            index!.Recommendations.OrderBy(r => r.Priority).ThenBy(r => r.Title, StringComparer.Ordinal).ToList(),
            index.Recommendations.ToList());
    }

    [SkippableFact]
    public async Task Navigation_replay_severity_consistent_with_replay_workbench()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var navigation = await GetNavigationIndexAsync();
        var replayResponse = await _client.GetAsync(ReplayWorkbenchBase);
        replayResponse.EnsureSuccessStatusCode();
        var replay = await replayResponse.Content.ReadFromJsonAsync<OperationalReplayWorkbenchDto>();

        Assert.NotNull(navigation);
        Assert.NotNull(replay);

        var replaySection = navigation!.Sections.Single(s =>
            s.SectionName == OperationalNavigationAggregation.SectionReplayStability);

        if (replay!.PressureSummary.InstabilityLevel >= OperationalReplayPressureLevel.High)
            Assert.True(replaySection.Severity >= OperationalNavigationSeverity.Elevated);
    }

    [SkippableFact]
    public async Task Navigation_routes_endpoint_returns_bounded_route_list()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var routes = await GetNavigationRoutesAsync();
        Assert.NotNull(routes);
        Assert.InRange(routes!.Count, 5, 6);
        Assert.All(routes, route => Assert.False(string.IsNullOrWhiteSpace(route.DisplayName)));
        Assert.All(routes, route => Assert.False(string.IsNullOrWhiteSpace(route.OperatorSummary)));
    }

    [SkippableFact]
    public async Task Navigation_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var before = await GetNavigationIndexAsync();
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();
        var after = await GetNavigationIndexAsync();

        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(6, before!.Sections.Count);
        Assert.Equal(6, after!.Sections.Count);
        Assert.False(string.IsNullOrWhiteSpace(before.Summary));
        Assert.False(string.IsNullOrWhiteSpace(after.Summary));
    }

    private void ResetOperationalTrendWindow()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
    }

    private async Task<OperationalNavigationIndexDto?> GetNavigationIndexAsync()
    {
        var response = await _client.GetAsync(NavigationBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalNavigationIndexDto>();
    }

    private async Task<IReadOnlyList<OperationalNavigationRouteDto>?> GetNavigationRoutesAsync()
    {
        var response = await _client.GetAsync(NavigationRoutesBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalNavigationRouteDto>>();
    }
}
