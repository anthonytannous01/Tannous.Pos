using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
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
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalSurvivabilityIntegrationTests : IntegrationTestBase
{
    private const string ResilienceReportBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string ResiliencePostureSummaryBase = "/api/v1.0/internal/operational-audit/resilience/posture/summary";
    private const string FragilityBase = "/api/v1.0/internal/operational-audit/resilience/fragility";
    private const string ConvergenceBase = "/api/v1.0/internal/operational-audit/convergence";
    private const string TopologyBase = "/api/v1.0/internal/operational-audit/topology";

    public OperationalSurvivabilityIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Resilience_report_returns_bounded_survivability_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(ConvergenceBase);
        await _client.GetAsync(TopologyBase);

        var response = await _client.GetAsync(ResilienceReportBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalResilienceReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.SurvivabilityAnalyses.Count <= OperationalResilienceAggregation.MaxSurvivabilityAnalyses);
        Assert.True(dto.ContainmentDurabilities.Count <= OperationalResilienceAggregation.MaxContainmentDurabilities);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.ResilienceContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Resilience_posture_summary_returns_survivability_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ResiliencePostureSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalResiliencePostureSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantResilienceArea));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrongestContainmentZone));
    }

    [SkippableFact]
    public async Task Operational_fragility_returns_deterministic_structure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(FragilityBase);
        response.EnsureSuccessStatusCode();

        var fragilities = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalFragilityDto>>();
        Assert.NotNull(fragilities);
        Assert.True(fragilities!.Count <= OperationalResilienceAggregation.MaxFragilities);
    }

    [SkippableFact]
    public async Task Resilience_cognition_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetResilienceReportAsync();
        var second = await GetResilienceReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.SurvivabilityState, second!.SurvivabilityState);
    }

    [SkippableFact]
    public async Task Resilience_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(ResilienceReportBase);

        var report = await GetResilienceReportAsync();
        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report!.ResilienceContinuity.SurvivabilityConsistency));
    }

    [SkippableFact]
    public async Task Resilience_cognition_avoids_chaos_and_probabilistic_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetResilienceReportAsync();
        var summary = await GetResiliencePostureSummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Chaos", "FaultInjection", "Probabilistic", "MachineLearning", "Forecast" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Resilience_cognition_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalResilienceAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(ResilienceReportBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalResilienceCognitionSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalResilienceAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalResilienceReportDto?> GetResilienceReportAsync()
    {
        var response = await _client.GetAsync(ResilienceReportBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalResilienceReportDto>();
    }

    private async Task<OperationalResiliencePostureSummaryDto?> GetResiliencePostureSummaryAsync()
    {
        var response = await _client.GetAsync(ResiliencePostureSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalResiliencePostureSummaryDto>();
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
    }
}
