using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalEquilibriumIntegrationTests : IntegrationTestBase
{
    private const string EquilibriumReportBase = "/api/v1.0/internal/operational-audit/equilibrium";
    private const string EquilibriumSummaryBase = "/api/v1.0/internal/operational-audit/equilibrium/summary";
    private const string ImbalancesBase = "/api/v1.0/internal/operational-audit/equilibrium/imbalances";
    private const string StrategyBase = "/api/v1.0/internal/operational-audit/strategy";
    private const string AttentionBase = "/api/v1.0/internal/operational-audit/attention";

    public OperationalEquilibriumIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Equilibrium_report_returns_bounded_balance_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(StrategyBase);
        await _client.GetAsync(AttentionBase);

        var response = await _client.GetAsync(EquilibriumReportBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalEquilibriumReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.SystemicBalances.Count <= OperationalEquilibriumAggregation.MaxSystemicBalances);
        Assert.True(dto.Imbalances.Count <= OperationalEquilibriumAggregation.MaxImbalances);
        Assert.True(dto.PressureDistributions.Count <= OperationalEquilibriumAggregation.MaxPressureDistributions);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.EquilibriumContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Equilibrium_summary_returns_systemic_balance_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(EquilibriumSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalEquilibriumSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrongestStabilizationBalance));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestImbalancePressure));
    }

    [SkippableFact]
    public async Task Operational_imbalances_returns_deterministic_structure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ImbalancesBase);
        response.EnsureSuccessStatusCode();

        var imbalances = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalImbalanceDto>>();
        Assert.NotNull(imbalances);
        Assert.True(imbalances!.Count <= OperationalEquilibriumAggregation.MaxImbalances);
    }

    [SkippableFact]
    public async Task Equilibrium_state_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetEquilibriumReportAsync();
        var second = await GetEquilibriumReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.EquilibriumState, second!.EquilibriumState);
    }

    [SkippableFact]
    public async Task Equilibrium_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(EquilibriumReportBase);

        var report = await GetEquilibriumReportAsync();
        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report!.EquilibriumContinuity.StabilizationBalanceConsistency));
    }

    [SkippableFact]
    public async Task Equilibrium_avoids_optimization_and_probabilistic_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetEquilibriumReportAsync();
        var summary = await GetEquilibriumSummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Optimization", "Probabilistic", "MachineLearning", "ControlTheory" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Equilibrium_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalEquilibriumAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(EquilibriumReportBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalEquilibriumSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalEquilibriumAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalEquilibriumReportDto?> GetEquilibriumReportAsync()
    {
        var response = await _client.GetAsync(EquilibriumReportBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalEquilibriumReportDto>();
    }

    private async Task<OperationalEquilibriumSummaryDto?> GetEquilibriumSummaryAsync()
    {
        var response = await _client.GetAsync(EquilibriumSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalEquilibriumSummaryDto>();
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalCausalitySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalSituationSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalSimulationSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalPlaybookSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalPatternSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIntegritySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalExperienceSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalDigestSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalEvolutionSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTopologySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalConvergenceSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalResilienceCognitionSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalAttentionSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalStrategySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalEquilibriumSnapshotStore>().Clear();
    }
}
