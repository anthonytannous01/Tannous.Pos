using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
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

public class OperationalExperienceGraphIntegrationTests : IntegrationTestBase
{
    private const string GraphBase = "/api/v1.0/internal/operational-audit/experience-graph";
    private const string TraversalBase = "/api/v1.0/internal/operational-audit/experience-graph/traversal";
    private const string NavigationBase = "/api/v1.0/internal/operational-audit/experience-graph/navigation";

    public OperationalExperienceGraphIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Experience_graph_returns_bounded_operational_relationships()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(GraphBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalExperienceGraphDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Relationships.Count <= OperationalExperienceGraphAggregation.MaxRelationships);
        Assert.False(string.IsNullOrWhiteSpace(dto.RecommendedEntryPoint));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.InvestigationContinuity.InvestigationTheme));
    }

    [SkippableFact]
    public async Task Traversal_paths_returns_bounded_sequenced_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(TraversalBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalExperienceTraversalPathsDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.TraversalPaths.Count <= OperationalExperienceGraphAggregation.MaxTraversalPaths);
        Assert.Equal(dto.PathCount, dto.TraversalPaths.Count);
        Assert.All(dto.TraversalPaths, p => Assert.NotEmpty(p.RecommendedSequence));
    }

    [SkippableFact]
    public async Task Contextual_navigation_returns_operator_guidance()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(NavigationBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalContextualNavigationDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.CurrentOperationalFocus));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecommendedNextSurface));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorInterpretation));
        Assert.True(dto.RelatedOperationalAreas.Count <= OperationalExperienceGraphAggregation.MaxRelatedAreas);
    }

    [SkippableFact]
    public async Task Experience_graph_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetExperienceGraphAsync();
        var second = await GetExperienceGraphAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DominantOperationalContext, second!.DominantOperationalContext);
        Assert.Equal(
            first.Relationships.Select(r => r.SourceSurface + r.TargetSurface),
            second.Relationships.Select(r => r.SourceSurface + r.TargetSurface));
    }

    [SkippableFact]
    public async Task Traversal_includes_replay_investigation_flow()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var paths = await GetTraversalPathsAsync();
        Assert.NotNull(paths);

        var replayPath = paths!.TraversalPaths.FirstOrDefault(p =>
            p.PathId == OperationalExperienceGraphAggregation.PathReplayInvestigation);
        Assert.NotNull(replayPath);
        Assert.Contains(OperationalExperienceGraphAggregation.SurfaceTimeline, replayPath!.RecommendedSequence);
        Assert.Contains(OperationalExperienceGraphAggregation.SurfaceCausality, replayPath.RecommendedSequence);
        Assert.Contains(OperationalExperienceGraphAggregation.SurfaceSimulation, replayPath.RecommendedSequence);
        Assert.Contains(OperationalExperienceGraphAggregation.SurfacePlaybooks, replayPath.RecommendedSequence);
        Assert.Contains(OperationalExperienceGraphAggregation.SurfaceIntegrity, replayPath.RecommendedSequence);
    }

    [SkippableFact]
    public async Task Experience_graph_avoids_governance_and_ml_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var graph = await GetExperienceGraphAsync();
        var navigation = await GetContextualNavigationAsync();
        Assert.NotNull(graph);
        Assert.NotNull(navigation);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "GraphDatabase", "MachineLearning", "WorkflowEngine" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, graph!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, navigation!.OperatorInterpretation, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Experience_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalExperienceGraphAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(GraphBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalExperienceSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalExperienceGraphAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalExperienceGraphDto?> GetExperienceGraphAsync()
    {
        var response = await _client.GetAsync(GraphBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalExperienceGraphDto>();
    }

    private async Task<OperationalExperienceTraversalPathsDto?> GetTraversalPathsAsync()
    {
        var response = await _client.GetAsync(TraversalBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalExperienceTraversalPathsDto>();
    }

    private async Task<OperationalContextualNavigationDto?> GetContextualNavigationAsync()
    {
        var response = await _client.GetAsync(NavigationBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalContextualNavigationDto>();
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
    }
}
