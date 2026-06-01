using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalTimelineIntegrationTests : IntegrationTestBase
{
    private const string TimelineBase = "/api/v1.0/internal/operational-audit/timeline";
    private const string TimelineCorrelationsBase = "/api/v1.0/internal/operational-audit/timeline/correlations";

    public OperationalTimelineIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Timeline_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(TimelineBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalTimelineDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.TimelineNote));
        Assert.True(dto.EventCount <= OperationalTimelineAggregation.MaxTimelineEvents);
        Assert.True(dto.AttentionItems.Count <= OperationalTimelineAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Timeline_window_retains_max_twenty_five_events_fifo()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 30; i++)
            await _client.GetAsync(TimelineBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>();
        var events = store.GetEvents();

        Assert.Equal(OperationalTimelineAggregation.MaxTimelineEvents, events.Count);
    }

    [SkippableFact]
    public async Task Timeline_events_are_chronologically_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TimelineBase);
        var timeline = await GetTimelineAsync();

        Assert.NotNull(timeline);
        Assert.Equal(
            timeline!.Events.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.Summary, StringComparer.Ordinal).ToList(),
            timeline.Events.ToList());
    }

    [SkippableFact]
    public async Task Timeline_correlations_are_bounded()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TimelineBase);
        var correlations = await GetCorrelationsAsync();

        Assert.NotNull(correlations);
        Assert.True(correlations!.Count <= OperationalTimelineAggregation.MaxCorrelations);
    }

    [SkippableFact]
    public async Task Timeline_seeded_transitions_generate_correlations()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>();
            store.SetLastCapture(new OperationalTimelineCaptureSnapshot
            {
                ActiveReplayPressure = 1,
                ReplayInstabilityLevel = "Elevated",
                ProtectiveModeActive = false
            });
        }

        await _client.GetAsync(TimelineBase);
        var correlations = await GetCorrelationsAsync();

        Assert.NotNull(correlations);
        Assert.NotEmpty(correlations!);
    }

    [SkippableFact]
    public async Task Timeline_does_not_retain_payload_fields_in_window_store()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TimelineBase);

        using var scope = _factory.Services.CreateScope();
        var serialized = System.Text.Json.JsonSerializer.Serialize(
            scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().GetEvents());

        Assert.DoesNotContain("Payload", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Receipt", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityId", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Export", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Timeline_routes_use_existing_operational_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        var timeline = await GetTimelineAsync();
        Assert.NotNull(timeline);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalTimelineAggregation.RouteDashboard,
            OperationalTimelineAggregation.RouteReconciliationWorkbench,
            OperationalTimelineAggregation.RouteInventoryWorkbench,
            OperationalTimelineAggregation.RouteReplayWorkbench,
            OperationalTimelineAggregation.RouteTrendSummary
        };

        Assert.All(timeline!.Events, e => Assert.Contains(e.SuggestedRoute, allowed));
    }

    [SkippableFact]
    public async Task Timeline_reset_clears_window_store_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTimelineStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TimelineBase);
        ResetOperationalTimelineStores();

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>();
        Assert.Empty(store.GetEvents());
        Assert.Null(store.GetLastCapture());
    }

    private void ResetOperationalTimelineStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
    }

    private async Task<OperationalTimelineDto?> GetTimelineAsync()
    {
        var response = await _client.GetAsync(TimelineBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalTimelineDto>();
    }

    private async Task<IReadOnlyList<OperationalTimelineCorrelationDto>?> GetCorrelationsAsync()
    {
        var response = await _client.GetAsync(TimelineCorrelationsBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalTimelineCorrelationDto>>();
    }
}
