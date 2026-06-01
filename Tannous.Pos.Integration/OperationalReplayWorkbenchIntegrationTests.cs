using System.Net.Http.Json;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Integration;

public class OperationalReplayWorkbenchIntegrationTests : IntegrationTestBase
{
    private const string ReplayWorkbenchBase = "/api/v1.0/internal/operational-audit/replay-workbench/pressure";
    private const string DashboardBase = "/api/v1.0/internal/operational-audit/dashboard";
    private const string ReconciliationWorkbenchBase = "/api/v1.0/internal/operational-audit/workbench/reconciliation";
    private const string InventoryWorkbenchBase = "/api/v1.0/internal/operational-audit/inventory-workbench/drift";

    public OperationalReplayWorkbenchIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Replay_workbench_pressure_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ReplayWorkbenchBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalReplayWorkbenchDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto!.PressureSummary);
        Assert.NotNull(dto.Stabilization);
        Assert.NotNull(dto.RecoveryConfidence);
        Assert.False(string.IsNullOrWhiteSpace(dto.WorkbenchNote));
        Assert.True(dto.Hotspots.Count <= OperationalReplayWorkbenchAggregation.MaxHotspots);
        Assert.True(dto.AttentionItems.Count <= OperationalReplayWorkbenchAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Replay_workbench_hotspots_are_deterministically_ordered_and_capped()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetReplayWorkbenchAsync();
        var second = await GetReplayWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Hotspots.Select(h => h.Category), second!.Hotspots.Select(h => h.Category));
        Assert.True(first.Hotspots.Count <= OperationalReplayWorkbenchAggregation.MaxHotspots);
        Assert.Equal(
            first.Hotspots
                .OrderByDescending(h => h.Severity)
                .ThenByDescending(h => h.PressureCount)
                .ThenBy(h => h.Category, StringComparer.Ordinal)
                .ToList(),
            first.Hotspots.ToList());
    }

    [SkippableFact]
    public async Task Replay_workbench_attention_items_are_deterministically_ordered_and_capped()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetReplayWorkbenchAsync();
        var second = await GetReplayWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.AttentionItems.Select(i => i.Title), second!.AttentionItems.Select(i => i.Title));
        Assert.True(first.AttentionItems.Count <= OperationalReplayWorkbenchAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Replay_workbench_recovery_confidence_is_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetReplayWorkbenchAsync();
        var second = await GetReplayWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.RecoveryConfidence.Confidence, second!.RecoveryConfidence.Confidence);
        Assert.Equal(first.Stabilization.StabilizationActive, second.Stabilization.StabilizationActive);
        Assert.Equal(first.PressureSummary.InstabilityLevel, second.PressureSummary.InstabilityLevel);
    }

    [SkippableFact]
    public async Task Replay_workbench_protective_mode_consistent_with_dashboard_and_workbenches()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var replay = await GetReplayWorkbenchAsync();
        var dashboard = await GetDashboardAsync();
        var reconciliation = await GetReconciliationWorkbenchAsync();
        var inventory = await GetInventoryWorkbenchAsync();

        Assert.NotNull(replay);
        Assert.NotNull(dashboard);
        Assert.NotNull(reconciliation);
        Assert.NotNull(inventory);

        var expectedProtective = dashboard!.Pressure.ProtectiveModeActive
            || reconciliation!.ReplayRisk.ProtectiveModeActive
            || inventory!.DriftSummary.ProtectiveModeActive;

        if (expectedProtective)
        {
            Assert.True(
                replay!.PressureSummary.ProtectiveModeVisible
                || replay.Stabilization.ProtectiveContainmentActive);
        }
    }

    [SkippableFact]
    public async Task Replay_workbench_reuses_upstream_caches_without_new_categories()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var warm = await GetReplayWorkbenchAsync();
        ClearOperationalDiagnosticsUpstreamCaches();
        var afterClear = await GetReplayWorkbenchAsync();

        Assert.NotNull(warm);
        Assert.NotNull(afterClear);
        Assert.Equal(warm!.PressureSummary.InstabilityLevel, afterClear!.PressureSummary.InstabilityLevel);
        Assert.True(afterClear.GeneratedAtUtc >= warm.GeneratedAtUtc);
    }

    [SkippableFact]
    public async Task Replay_workbench_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var beforeReset = await GetReplayWorkbenchAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var afterReset = await GetReplayWorkbenchAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.Equal(beforeReset!.RecoveryConfidence.Confidence, afterReset!.RecoveryConfidence.Confidence);
        Assert.True(afterReset.Hotspots.Count <= OperationalReplayWorkbenchAggregation.MaxHotspots);
    }

    private async Task<OperationalReplayWorkbenchDto?> GetReplayWorkbenchAsync()
    {
        var response = await _client.GetAsync(ReplayWorkbenchBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalReplayWorkbenchDto>();
    }

    private async Task<OperationalDashboardSummaryDto?> GetDashboardAsync()
    {
        var response = await _client.GetAsync(DashboardBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalDashboardSummaryDto>();
    }

    private async Task<OperationalReconciliationWorkbenchDto?> GetReconciliationWorkbenchAsync()
    {
        var response = await _client.GetAsync(ReconciliationWorkbenchBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalReconciliationWorkbenchDto>();
    }

    private async Task<OperationalInventoryWorkbenchDto?> GetInventoryWorkbenchAsync()
    {
        var response = await _client.GetAsync(InventoryWorkbenchBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalInventoryWorkbenchDto>();
    }
}
