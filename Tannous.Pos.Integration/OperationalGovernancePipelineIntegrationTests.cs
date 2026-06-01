using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.Audit.Governance.Modules;

namespace Tannous.Pos.Integration;

public class OperationalGovernancePipelineIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernancePipelineIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Pipeline_backed_endpoints_return_stable_governance_overview()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetGovernanceOverviewAsync();
        var second = await GetGovernanceOverviewAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ReadinessState, second!.ReadinessState);
        Assert.Equal(first.PressureSeverity, second.PressureSeverity);
        Assert.Equal(first.DegradationState, second.DegradationState);
    }

    [SkippableFact]
    public async Task Consistency_and_invalidation_projections_remain_aligned_after_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        SeedStickyPressure();
        ResetOperationalGovernanceStabilization();

        var recovery = await GetConsistencyRecoveryAsync();
        var invalidation = await GetInvalidationAuditAsync();

        Assert.NotNull(recovery);
        Assert.NotNull(invalidation);
        AssertNoDuplicateExplainabilityCodes(recovery!.ReasonCodes);
        AssertNoDuplicateExplainabilityCodes(invalidation!.ReasonCodes);
    }

    [SkippableFact]
    public void Module_dependency_graph_is_acyclic_and_pipeline_stage_count_is_bounded()
    {
        var graph = OperationalGovernanceModuleRegistry.DependencyGraph();
        Assert.False(OperationalGovernanceDependencyRules.HasCircularDependencies(graph));
        Assert.True(OperationalGovernanceProjectionPipeline.StageOrder.Count <= OperationalGovernanceComplexityMetrics.MaxPipelineStageCount);
    }

    [SkippableFact]
    public async Task Pressure_convergence_outputs_match_across_repeated_pipeline_queries()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetPressureConvergenceAsync();
        var second = await GetPressureConvergenceAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ConvergenceClassification, second!.ConvergenceClassification);
        Assert.Equal(first.ConvergenceScore, second.ConvergenceScore);
        Assert.Equal(first.ReasonCodes, second.ReasonCodes);
    }

    private void SeedStickyPressure()
    {
        using var scope = _factory.Services.CreateScope();
        var resilience = scope.ServiceProvider.GetRequiredService<IOperationalResilienceDiagnosticsService>();
        resilience.NoteQueryPressure(dateRangeClamped: true, pageSizeClamped: true);
    }

    private static void AssertNoDuplicateExplainabilityCodes(IReadOnlyList<string> codes)
    {
        var distinct = codes.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    private async Task<OperationalCacheGovernanceOverviewDto?> GetGovernanceOverviewAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();
    }

    private async Task<OperationalCacheConsistencyRecoveryDto?> GetConsistencyRecoveryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/consistency-recovery");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyRecoveryDto>();
    }

    private async Task<OperationalCacheInvalidationAuditDto?> GetInvalidationAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/invalidation-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationAuditDto>();
    }

    private async Task<OperationalPressureConvergenceDto?> GetPressureConvergenceAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/pressure-convergence");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPressureConvergenceDto>();
    }
}
