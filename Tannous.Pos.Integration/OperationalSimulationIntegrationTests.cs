using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalSimulationIntegrationTests : IntegrationTestBase
{
    private const string SimulationBase = "/api/v1.0/internal/operational-audit/simulation";
    private const string SimulationSummaryBase = "/api/v1.0/internal/operational-audit/simulation/summary";
    private const string SimulationOutlookBase = "/api/v1.0/internal/operational-audit/simulation/outlook";

    public OperationalSimulationIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Simulation_scenarios_returns_bounded_hypothetical_analysis()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SimulationBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalSimulationScenariosDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Scenarios.Count <= OperationalSimulationAggregation.MaxScenarios);
        Assert.True(dto.StabilizationPaths.Count <= OperationalSimulationAggregation.MaxStabilizationPaths);
        Assert.True(dto.DegradationPaths.Count <= OperationalSimulationAggregation.MaxDegradationPaths);
        Assert.True(dto.LeveragePoints.Count <= OperationalSimulationAggregation.MaxLeveragePoints);
    }

    [SkippableFact]
    public async Task Simulation_summary_returns_leverage_and_constraint()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SimulationSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalSimulationSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestLeverageArea));
    }

    [SkippableFact]
    public async Task Simulation_outlook_returns_stabilization_and_degradation_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SimulationOutlookBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalSimulationOutlookDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.MostLikelyStabilizationPath));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestRiskDegradationPath));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrongestLeveragePoint));
    }

    [SkippableFact]
    public async Task Simulation_scenarios_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetSimulationScenariosAsync();
        var second = await GetSimulationScenariosAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Scenarios.Select(s => s.ScenarioId), second!.Scenarios.Select(s => s.ScenarioId));
    }

    [SkippableFact]
    public async Task Simulation_avoids_governance_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var scenarios = await GetSimulationScenariosAsync();
        var summary = await GetSimulationSummaryAsync();
        Assert.NotNull(scenarios);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Probabilistic" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(scenarios!.Scenarios, s => Assert.DoesNotContain(term, s.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [SkippableFact]
    public async Task Simulation_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalSimulationAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(SimulationBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalSimulationSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalSimulationAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalSimulationScenariosDto?> GetSimulationScenariosAsync()
    {
        var response = await _client.GetAsync(SimulationBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalSimulationScenariosDto>();
    }

    private async Task<OperationalSimulationSummaryDto?> GetSimulationSummaryAsync()
    {
        var response = await _client.GetAsync(SimulationSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalSimulationSummaryDto>();
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
    }
}
