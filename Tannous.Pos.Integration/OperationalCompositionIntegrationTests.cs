using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Integration;

public class OperationalCompositionIntegrationTests : IntegrationTestBase
{
    private const string DashboardBase = "/api/v1.0/internal/operational-audit/dashboard";
    private const string ReconciliationWorkbenchBase = "/api/v1.0/internal/operational-audit/workbench/reconciliation";
    private const string InventoryWorkbenchBase = "/api/v1.0/internal/operational-audit/inventory-workbench/drift";
    private const string ReplayWorkbenchBase = "/api/v1.0/internal/operational-audit/replay-workbench/pressure";

    public OperationalCompositionIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Composition_hub_reuses_upstream_summaries_within_single_scope()
    {
        await InitializeDatabaseAsync();
        ResetOperationalGovernanceDiagnosticsState();

        using var scope = _factory.Services.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IOperationalReadCompositionHub>();

        await hub.GetResilienceSummaryAsync();
        await hub.GetResilienceSummaryAsync();
        await hub.GetReconciliationSummaryAsync();
        await hub.GetReconciliationSummaryAsync();

        Assert.True(hub.Context.CompositionReuseHits >= 2);
    }

    [SkippableFact]
    public async Task Replay_workbench_endpoint_preserves_contract_and_consistency_with_dashboard()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var replay = await GetReplayWorkbenchAsync();
        var dashboard = await GetDashboardAsync();

        Assert.NotNull(replay);
        Assert.NotNull(dashboard);
        Assert.True(replay!.Hotspots.Count <= OperationalReplayWorkbenchAggregation.MaxHotspots);
        Assert.True(replay.AttentionItems.Count <= OperationalReplayWorkbenchAggregation.MaxAttentionItems);

        if (dashboard!.Pressure.ProtectiveModeActive)
        {
            Assert.True(
                replay.PressureSummary.ProtectiveModeVisible
                || replay.Stabilization.ProtectiveContainmentActive);
        }
    }

    [SkippableFact]
    public async Task Workbench_endpoints_remain_consistent_after_composition_refactor()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var reconciliation = await GetReconciliationWorkbenchAsync();
        var inventory = await GetInventoryWorkbenchAsync();
        var dashboard = await GetDashboardAsync();

        Assert.NotNull(reconciliation);
        Assert.NotNull(inventory);
        Assert.NotNull(dashboard);

        Assert.Equal(
            dashboard!.Activity.UnresolvedReconciliationCount,
            reconciliation!.Queue.UnresolvedConflicts);
        Assert.Equal(
            dashboard.Activity.InventoryDriftRiskCount,
            inventory!.DriftSummary.TotalInventoryDriftConflicts);
    }

    [SkippableFact]
    public async Task Composition_hub_builds_snapshot_without_nested_workbench_service_calls()
    {
        await InitializeDatabaseAsync();
        ResetOperationalGovernanceDiagnosticsState();

        using var scope = _factory.Services.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IOperationalReadCompositionHub>();

        var snapshot = await hub.BuildSnapshotAsync();
        await hub.GetInventoryWorkbenchViewAsync();
        await hub.GetInventoryWorkbenchViewAsync();

        Assert.NotNull(snapshot.Resilience);
        Assert.True(hub.Context.CompositionNestedReadAvoidanceCount >= 1);
        Assert.True(hub.Context.CompositionReuseHits >= 1);
    }

    [SkippableFact]
    public async Task Composition_workbench_views_stable_on_repeated_endpoint_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var firstReplay = await GetReplayWorkbenchAsync();
        var secondReplay = await GetReplayWorkbenchAsync();

        Assert.NotNull(firstReplay);
        Assert.NotNull(secondReplay);
        Assert.Equal(firstReplay!.RecoveryConfidence.Confidence, secondReplay!.RecoveryConfidence.Confidence);
        Assert.Equal(firstReplay.PressureSummary.InstabilityLevel, secondReplay.PressureSummary.InstabilityLevel);
    }

    [SkippableFact]
    public async Task Composition_refactor_works_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var before = await GetDashboardAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var after = await GetDashboardAsync();

        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before!.Health.State, after!.Health.State);
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
