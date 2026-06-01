using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalEvolutionIntegrationTests : IntegrationTestBase
{
    private const string EvolutionBase = "/api/v1.0/internal/operational-audit/evolution";
    private const string EvolutionSummaryBase = "/api/v1.0/internal/operational-audit/evolution/summary";
    private const string MomentumBase = "/api/v1.0/internal/operational-audit/evolution/momentum";
    private const string DigestBase = "/api/v1.0/internal/operational-audit/digest";

    public OperationalEvolutionIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Evolution_timeline_returns_bounded_transition_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(DigestBase);

        var response = await _client.GetAsync(EvolutionBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalEvolutionTimelineDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Transitions.Count <= OperationalEvolutionAggregation.MaxTransitions);
        Assert.True(dto.Phases.Count <= OperationalEvolutionAggregation.MaxPhases);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecoveryMomentum));
        Assert.False(string.IsNullOrWhiteSpace(dto.EvolutionContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Evolution_summary_returns_momentum_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(EvolutionSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalEvolutionSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantTransition));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecoveryDirection));
    }

    [SkippableFact]
    public async Task Momentum_analysis_returns_deterministic_interpretation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(MomentumBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalMomentumAnalysisDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.RecoveryMomentum));
        Assert.False(string.IsNullOrWhiteSpace(dto.EscalationMomentum));
        Assert.False(string.IsNullOrWhiteSpace(dto.StabilizationMomentum));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperationalConfidence));
    }

    [SkippableFact]
    public async Task Evolution_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetEvolutionTimelineAsync();
        var second = await GetEvolutionTimelineAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DominantEvolutionDirection, second!.DominantEvolutionDirection);
        Assert.Equal(
            first.Transitions.Select(t => t.TransitionId),
            second.Transitions.Select(t => t.TransitionId));
    }

    [SkippableFact]
    public async Task Evolution_detects_digest_state_transition_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(DigestBase);

        var timeline = await GetEvolutionTimelineAsync();
        Assert.NotNull(timeline);
        Assert.NotEmpty(timeline!.Phases);
    }

    [SkippableFact]
    public async Task Evolution_avoids_governance_and_forecasting_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var timeline = await GetEvolutionTimelineAsync();
        var summary = await GetEvolutionSummaryAsync();
        Assert.NotNull(timeline);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "Forecast", "TimeSeries", "Predictive" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, timeline!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Evolution_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalEvolutionAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(EvolutionBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalEvolutionSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalEvolutionAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalEvolutionTimelineDto?> GetEvolutionTimelineAsync()
    {
        var response = await _client.GetAsync(EvolutionBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalEvolutionTimelineDto>();
    }

    private async Task<OperationalEvolutionSummaryDto?> GetEvolutionSummaryAsync()
    {
        var response = await _client.GetAsync(EvolutionSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalEvolutionSummaryDto>();
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
    }
}
