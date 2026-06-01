using System.Net.Http.Json;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Integration;

public class OperationalWorkbenchIntegrationTests : IntegrationTestBase
{
    private const string WorkbenchBase = "/api/v1.0/internal/operational-audit/workbench/reconciliation";
    private const string DashboardBase = "/api/v1.0/internal/operational-audit/dashboard";

    public OperationalWorkbenchIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Workbench_reconciliation_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(WorkbenchBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalReconciliationWorkbenchDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto!.Queue);
        Assert.NotNull(dto.ReplayRisk);
        Assert.NotNull(dto.InventoryDrift);
        Assert.False(string.IsNullOrWhiteSpace(dto.WorkbenchNote));
        Assert.True(dto.Hotspots.Count <= OperationalReconciliationWorkbenchAggregation.MaxHotspots);
        Assert.True(dto.AttentionItems.Count <= OperationalReconciliationWorkbenchAggregation.MaxAttentionItems);
    }

    [SkippableFact]
    public async Task Workbench_attention_items_are_deterministically_ordered_and_capped()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetWorkbenchAsync();
        var second = await GetWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.AttentionItems.Select(i => i.Title), second!.AttentionItems.Select(i => i.Title));
        Assert.Equal(
            first.AttentionItems.OrderBy(i => i.Priority).ThenBy(i => i.Title, StringComparer.Ordinal).ToList(),
            first.AttentionItems.ToList());
    }

    [SkippableFact]
    public async Task Workbench_hotspots_are_deterministically_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetWorkbenchAsync();
        var second = await GetWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Hotspots.Select(h => h.Category), second!.Hotspots.Select(h => h.Category));
        Assert.Equal(
            first.Hotspots
                .OrderByDescending(h => h.Severity)
                .ThenByDescending(h => h.PressureCount)
                .ThenBy(h => h.Category, StringComparer.Ordinal)
                .ToList(),
            first.Hotspots.ToList());
    }

    [SkippableFact]
    public async Task Workbench_replay_risk_projection_is_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetWorkbenchAsync();
        var second = await GetWorkbenchAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ReplayRisk.InstabilityLevel, second!.ReplayRisk.InstabilityLevel);
        Assert.Equal(first.ReplayRisk.ProtectiveModeActive, second.ReplayRisk.ProtectiveModeActive);
        Assert.Equal(first.ReplayRisk.ReplayEscalationObserved, second.ReplayRisk.ReplayEscalationObserved);
        Assert.Equal(first.ReplayRisk.StabilizationRecovering, second.ReplayRisk.StabilizationRecovering);
    }

    [SkippableFact]
    public async Task Workbench_and_dashboard_remain_consistent_on_shared_activity_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var workbench = await GetWorkbenchAsync();
        var dashboard = await GetDashboardAsync();

        Assert.NotNull(workbench);
        Assert.NotNull(dashboard);
        Assert.Equal(
            dashboard!.Activity.UnresolvedReconciliationCount,
            workbench!.Queue.UnresolvedConflicts);
        Assert.Equal(
            dashboard.Activity.ReplayMismatchCount,
            workbench.Queue.ReplayRiskConflicts);
        Assert.Equal(
            dashboard.Activity.InventoryDriftRiskCount,
            workbench.Queue.InventoryDriftConflicts);
        Assert.Equal(
            dashboard.Pressure.ProtectiveModeActive,
            workbench.ReplayRisk.ProtectiveModeActive);
    }

    [SkippableFact]
    public async Task Workbench_reuses_upstream_caches_without_new_cache_categories()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var warm = await GetWorkbenchAsync();
        ClearOperationalDiagnosticsUpstreamCaches();
        var afterClear = await GetWorkbenchAsync();

        Assert.NotNull(warm);
        Assert.NotNull(afterClear);
        Assert.Equal(warm!.Queue.UnresolvedConflicts, afterClear!.Queue.UnresolvedConflicts);
        Assert.True(afterClear.GeneratedAtUtc >= warm.GeneratedAtUtc);
    }

    [SkippableFact]
    public async Task Workbench_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var beforeReset = await GetWorkbenchAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var afterReset = await GetWorkbenchAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.Equal(beforeReset!.Queue.ActiveConflicts, afterReset!.Queue.ActiveConflicts);
        Assert.True(afterReset.AttentionItems.Count <= OperationalReconciliationWorkbenchAggregation.MaxAttentionItems);
    }

    private async Task<OperationalReconciliationWorkbenchDto?> GetWorkbenchAsync()
    {
        var response = await _client.GetAsync(WorkbenchBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalReconciliationWorkbenchDto>();
    }

    private async Task<OperationalDashboardSummaryDto?> GetDashboardAsync()
    {
        var response = await _client.GetAsync(DashboardBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalDashboardSummaryDto>();
    }
}
