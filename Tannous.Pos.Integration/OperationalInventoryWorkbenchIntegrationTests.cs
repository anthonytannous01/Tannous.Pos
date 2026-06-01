using System.Net.Http.Json;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Integration;

public class OperationalInventoryWorkbenchIntegrationTests : IntegrationTestBase
{
    private const string InventoryWorkbenchBase = "/api/v1.0/internal/operational-audit/inventory-workbench/drift";
    private const string DashboardBase = "/api/v1.0/internal/operational-audit/dashboard";
    private const string ReconciliationWorkbenchBase = "/api/v1.0/internal/operational-audit/workbench/reconciliation";

    public OperationalInventoryWorkbenchIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Inventory_workbench_drift_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(InventoryWorkbenchBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalInventoryWorkbenchDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto!.DriftSummary);
        Assert.NotNull(dto.ResolutionReadiness);
        Assert.False(string.IsNullOrWhiteSpace(dto.WorkbenchNote));
        Assert.True(dto.Hotspots.Count <= OperationalInventoryWorkbenchAggregation.MaxHotspots);
        Assert.True(dto.AttentionItems.Count <= OperationalInventoryWorkbenchAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Inventory_workbench_hotspots_are_deterministically_ordered_and_capped()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetInventoryWorkbenchAsync();
        var second = await GetInventoryWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Hotspots.Select(h => h.Category), second!.Hotspots.Select(h => h.Category));
        Assert.True(first.Hotspots.Count <= OperationalInventoryWorkbenchAggregation.MaxHotspots);
        Assert.Equal(
            first.Hotspots
                .OrderByDescending(h => h.Severity)
                .ThenByDescending(h => h.PressureCount)
                .ThenBy(h => h.Category, StringComparer.Ordinal)
                .ToList(),
            first.Hotspots.ToList());
    }

    [SkippableFact]
    public async Task Inventory_workbench_attention_items_are_deterministically_ordered_and_capped()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetInventoryWorkbenchAsync();
        var second = await GetInventoryWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.AttentionItems.Select(i => i.Title), second!.AttentionItems.Select(i => i.Title));
        Assert.True(first.AttentionItems.Count <= OperationalInventoryWorkbenchAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Inventory_workbench_resolution_readiness_is_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetInventoryWorkbenchAsync();
        var second = await GetInventoryWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ResolutionReadiness.ResolutionState, second!.ResolutionReadiness.ResolutionState);
        Assert.Equal(first.ResolutionReadiness.ReadyForOperatorReview, second.ResolutionReadiness.ReadyForOperatorReview);
        Assert.Equal(first.ResolutionReadiness.BlockedByReplayPressure, second.ResolutionReadiness.BlockedByReplayPressure);
        Assert.Equal(first.ResolutionReadiness.BlockedByProtectiveMode, second.ResolutionReadiness.BlockedByProtectiveMode);
    }

    [SkippableFact]
    public async Task Inventory_workbench_protective_mode_consistent_with_dashboard_and_reconciliation_workbench()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var inventory = await GetInventoryWorkbenchAsync();
        var dashboard = await GetDashboardAsync();
        var reconciliation = await GetReconciliationWorkbenchAsync();

        Assert.NotNull(inventory);
        Assert.NotNull(dashboard);
        Assert.NotNull(reconciliation);
        Assert.Equal(
            dashboard!.Pressure.ProtectiveModeActive || reconciliation!.ReplayRisk.ProtectiveModeActive,
            inventory!.DriftSummary.ProtectiveModeActive);
        Assert.Equal(
            dashboard.Pressure.ProtectiveModeActive || reconciliation.ReplayRisk.ProtectiveModeActive,
            inventory.ResolutionReadiness.BlockedByProtectiveMode
                || inventory.DriftSummary.ProtectiveModeActive);
    }

    [SkippableFact]
    public async Task Inventory_workbench_drift_counts_consistent_with_dashboard_activity()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var inventory = await GetInventoryWorkbenchAsync();
        var dashboard = await GetDashboardAsync();

        Assert.NotNull(inventory);
        Assert.NotNull(dashboard);
        Assert.Equal(
            dashboard!.Activity.InventoryDriftRiskCount,
            inventory!.DriftSummary.TotalInventoryDriftConflicts);
    }

    [SkippableFact]
    public async Task Inventory_workbench_reuses_upstream_caches_without_new_categories()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var warm = await GetInventoryWorkbenchAsync();
        ClearOperationalDiagnosticsUpstreamCaches();
        var afterClear = await GetInventoryWorkbenchAsync();

        Assert.NotNull(warm);
        Assert.NotNull(afterClear);
        Assert.Equal(
            warm!.DriftSummary.TotalInventoryDriftConflicts,
            afterClear!.DriftSummary.TotalInventoryDriftConflicts);
        Assert.True(afterClear.GeneratedAtUtc >= warm.GeneratedAtUtc);
    }

    [SkippableFact]
    public async Task Inventory_workbench_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var beforeReset = await GetInventoryWorkbenchAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var afterReset = await GetInventoryWorkbenchAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.Equal(
            beforeReset!.DriftSummary.DriftSeverity,
            afterReset!.DriftSummary.DriftSeverity);
        Assert.True(afterReset.Hotspots.Count <= OperationalInventoryWorkbenchAggregation.MaxHotspots);
    }

    private async Task<OperationalInventoryWorkbenchDto?> GetInventoryWorkbenchAsync()
    {
        var response = await _client.GetAsync(InventoryWorkbenchBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalInventoryWorkbenchDto>();
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
}
