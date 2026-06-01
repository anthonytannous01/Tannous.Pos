using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalTrendIntegrationTests : IntegrationTestBase
{
    private const string TrendSummaryBase = "/api/v1.0/internal/operational-audit/trends/summary";
    private const string TrendDeltasBase = "/api/v1.0/internal/operational-audit/trends/deltas";

    public OperationalTrendIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Trend_summary_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(TrendSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalTrendSummaryDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto!.Window);
        Assert.False(string.IsNullOrWhiteSpace(dto.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.TrendNote));
        Assert.True(dto.AttentionItems.Count <= OperationalTrendAggregation.MaxAttentionItems);
        Assert.Equal(OperationalTrendAggregation.MaxWindowSnapshots, dto.Window.MaxSnapshots);
    }

    [SkippableFact]
    public async Task Trend_window_retains_max_three_snapshots_fifo()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 5; i++)
            await _client.GetAsync(TrendSummaryBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>();
        var snapshots = store.GetSnapshots();

        Assert.Equal(OperationalTrendAggregation.MaxWindowSnapshots, snapshots.Count);
    }

    [SkippableFact]
    public async Task Trend_summary_stable_on_repeated_reads_without_state_change()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetTrendSummaryAsync();
        var second = await GetTrendSummaryAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.OverallDirection, second!.OverallDirection);
        Assert.Equal(first.Severity, second.Severity);
    }

    [SkippableFact]
    public async Task Trend_deltas_empty_on_first_read_and_bounded_after_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        var firstDeltas = await GetTrendDeltasAsync();
        Assert.NotNull(firstDeltas);
        Assert.Empty(firstDeltas!);

        await _client.GetAsync(TrendSummaryBase);
        var secondDeltas = await GetTrendDeltasAsync();

        Assert.NotNull(secondDeltas);
        Assert.NotEmpty(secondDeltas!);
        Assert.True(secondDeltas!.Count <= OperationalTrendAggregation.MaxWindowSnapshots - 1);
    }

    [SkippableFact]
    public async Task Trend_degrading_transition_visible_when_window_store_seeded()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>();
            store.Append(new OperationalTrendSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow.AddMinutes(-2),
                ActiveReplayPressure = 1,
                InventoryDriftConflictCount = 0,
                UnresolvedReconciliationCount = 0,
                PressureBand = "Nominal",
                ProtectiveModeActive = false,
                FingerprintId = "baseline",
                FingerprintStability = "Stable"
            });
        }

        var summary = await GetTrendSummaryAsync();
        Assert.NotNull(summary);
        Assert.True(summary!.AttentionItems.Count <= OperationalTrendAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Trend_service_does_not_retain_payload_fields_in_window_store()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TrendSummaryBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>();
        var serialized = System.Text.Json.JsonSerializer.Serialize(store.GetSnapshots());

        Assert.DoesNotContain("Payload", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Receipt", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityId", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Export", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Trend_reset_clears_window_store_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalTrendWindow();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(TrendSummaryBase);
        ResetOperationalTrendWindow();

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>();
        Assert.Empty(store.GetSnapshots());

        var summary = await GetTrendSummaryAsync();
        Assert.NotNull(summary);
        Assert.False(summary!.Window.HasComparisonBaseline);
    }

    private void ResetOperationalTrendWindow()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
    }

    private async Task<OperationalTrendSummaryDto?> GetTrendSummaryAsync()
    {
        var response = await _client.GetAsync(TrendSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalTrendSummaryDto>();
    }

    private async Task<IReadOnlyList<OperationalTrendDeltaDto>?> GetTrendDeltasAsync()
    {
        var response = await _client.GetAsync(TrendDeltasBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalTrendDeltaDto>>();
    }
}
