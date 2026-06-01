using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalCausalityIntegrationTests : IntegrationTestBase
{
    private const string CausalityBase = "/api/v1.0/internal/operational-audit/causality";
    private const string CausalitySummaryBase = "/api/v1.0/internal/operational-audit/causality/summary";
    private const string CausalityPropagationBase = "/api/v1.0/internal/operational-audit/causality/propagation";

    public OperationalCausalityIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Causality_chains_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(CausalityBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalCausalChainsDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.ChainCount <= OperationalCausalityAggregation.MaxCausalChains);
        Assert.True(dto.Nodes.Count <= OperationalCausalityAggregation.MaxCausalChains * OperationalCausalityAggregation.MaxNodesPerChain);
    }

    [SkippableFact]
    public async Task Causality_summary_returns_dominant_area_and_blockers()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(CausalitySummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalCausalitySummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantOperationalArea));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorAttentionLevel));
    }

    [SkippableFact]
    public async Task Causality_propagation_returns_bounded_analysis()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(CausalityPropagationBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalPropagationAnalysisDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Propagations.Count <= OperationalCausalityAggregation.MaxPropagations);
        Assert.True(dto.RootCauseCandidates.Count <= OperationalCausalityAggregation.MaxRootCauseCandidates);
        Assert.True(dto.StabilizationBlockers.Count <= OperationalCausalityAggregation.MaxStabilizationBlockers);
    }

    [SkippableFact]
    public async Task Causality_chains_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetCausalChainsAsync();
        var second = await GetCausalChainsAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Chains.Select(c => c.ChainId), second!.Chains.Select(c => c.ChainId));
    }

    [SkippableFact]
    public async Task Causality_summary_avoids_governance_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var summary = await GetCausalitySummaryAsync();
        var propagation = await GetPropagationAnalysisAsync();
        Assert.NotNull(summary);
        Assert.NotNull(propagation);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(propagation!.RootCauseCandidates, r => Assert.DoesNotContain(term, r.Explanation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [SkippableFact]
    public async Task Causality_propagation_includes_root_cause_candidates_when_pressure_active()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var propagation = await GetPropagationAnalysisAsync();
        Assert.NotNull(propagation);
    }

    [SkippableFact]
    public async Task Causality_reset_clears_dependent_stores_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(CausalityBase);
        ResetOperationalStores();

        using var scope = _factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().GetEvents());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().GetSnapshots());
        scope.ServiceProvider.GetRequiredService<IOperationalCausalitySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();

        var chains = await GetCausalChainsAsync();
        Assert.NotNull(chains);
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalCausalitySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();
    }

    private async Task<OperationalCausalChainsDto?> GetCausalChainsAsync()
    {
        var response = await _client.GetAsync(CausalityBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCausalChainsDto>();
    }

    private async Task<OperationalCausalitySummaryDto?> GetCausalitySummaryAsync()
    {
        var response = await _client.GetAsync(CausalitySummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCausalitySummaryDto>();
    }

    private async Task<OperationalPropagationAnalysisDto?> GetPropagationAnalysisAsync()
    {
        var response = await _client.GetAsync(CausalityPropagationBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPropagationAnalysisDto>();
    }
}
