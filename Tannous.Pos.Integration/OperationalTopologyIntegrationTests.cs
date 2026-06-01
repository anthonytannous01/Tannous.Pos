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
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalTopologyIntegrationTests : IntegrationTestBase
{
    private const string TopologyBase = "/api/v1.0/internal/operational-audit/topology";
    private const string TopologySummaryBase = "/api/v1.0/internal/operational-audit/topology/summary";
    private const string ChainsBase = "/api/v1.0/internal/operational-audit/topology/chains";
    private const string DigestBase = "/api/v1.0/internal/operational-audit/digest";
    private const string EvolutionBase = "/api/v1.0/internal/operational-audit/evolution";

    public OperationalTopologyIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Topology_returns_bounded_dependency_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(DigestBase);
        await _client.GetAsync(EvolutionBase);

        var response = await _client.GetAsync(TopologyBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalTopologyDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Dependencies.Count <= OperationalTopologyAggregation.MaxDependencies);
        Assert.True(dto.Influences.Count <= OperationalTopologyAggregation.MaxInfluences);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.TopologyContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Topology_summary_returns_criticality_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(TopologySummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalTopologySummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantDependencyFlow));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestRiskDependency));
    }

    [SkippableFact]
    public async Task Dependency_chains_return_deterministic_structure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ChainsBase);
        response.EnsureSuccessStatusCode();

        var chains = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalDependencyChainDto>>();
        Assert.NotNull(chains);
        Assert.True(chains!.Count <= OperationalTopologyAggregation.MaxDependencyChains);
        foreach (var chain in chains)
        {
            Assert.False(string.IsNullOrWhiteSpace(chain.ChainId));
            Assert.False(string.IsNullOrWhiteSpace(chain.OperatorSummary));
        }
    }

    [SkippableFact]
    public async Task Topology_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetTopologyAsync();
        var second = await GetTopologyAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.TopologyState, second!.TopologyState);
        Assert.Equal(
            first.Dependencies.Select(d => d.SourceArea + "->" + d.TargetArea),
            second.Dependencies.Select(d => d.SourceArea + "->" + d.TargetArea));
    }

    [SkippableFact]
    public async Task Topology_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(TopologyBase);

        var topology = await GetTopologyAsync();
        Assert.NotNull(topology);
        Assert.False(string.IsNullOrWhiteSpace(topology!.TopologyContinuity.DependencyStability));
    }

    [SkippableFact]
    public async Task Topology_avoids_tracing_and_infrastructure_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var topology = await GetTopologyAsync();
        var summary = await GetTopologySummaryAsync();
        Assert.NotNull(topology);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "Tracing", "ServiceMesh", "GraphDatabase", "DistributedTracing" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, topology!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Topology_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalTopologyAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(TopologyBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalTopologySnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalTopologyAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalTopologyDto?> GetTopologyAsync()
    {
        var response = await _client.GetAsync(TopologyBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalTopologyDto>();
    }

    private async Task<OperationalTopologySummaryDto?> GetTopologySummaryAsync()
    {
        var response = await _client.GetAsync(TopologySummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalTopologySummaryDto>();
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
    }
}
