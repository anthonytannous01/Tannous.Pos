using System.Net.Http.Json;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceProductionReadinessIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceProductionReadinessIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Runtime_protection_includes_runtime_baseline_without_new_endpoint()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var dto = await GetRuntimeProtectionAsync();

        Assert.NotNull(dto);
        Assert.NotNull(dto!.RuntimeBaseline);
        Assert.False(string.IsNullOrWhiteSpace(dto.RuntimeBaseline.ExecutionBudgetState));
        Assert.False(string.IsNullOrWhiteSpace(dto.RuntimeBaseline.ProjectionTiming.TimingBand));
        Assert.True(dto.RuntimeBaseline.PipelineStageCount > 0);
        Assert.NotNull(dto.ProductionReadiness);
        Assert.False(string.IsNullOrWhiteSpace(dto.ProductionReadiness.ReadinessState));
    }

    [SkippableFact]
    public async Task Governance_overview_includes_production_readiness_classification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var overview = await GetGovernanceOverviewAsync();

        Assert.NotNull(overview);
        Assert.NotNull(overview!.ProductionReadiness);
        Assert.False(string.IsNullOrWhiteSpace(overview.ProductionReadiness.ReadinessState));
        AssertNoDuplicateExplainabilityCodes(overview.ProductionReadiness.ReadinessSignals);
    }

    [SkippableFact]
    public async Task Memoized_snapshot_reuse_preserves_fingerprint_determinism_within_request()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetGovernanceFingerprintAsync();
        var second = await GetGovernanceFingerprintAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.FingerprintHash, second!.FingerprintHash);
        Assert.Equal(
            first.ExplainabilityCodes.OrderBy(s => s, StringComparer.Ordinal),
            first.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Governance_reset_clears_stale_snapshot_reuse_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        await GetGovernanceSnapshotAsync();
        var reused = await GetGovernanceSnapshotAsync();
        Assert.NotNull(reused);
        Assert.Equal("Reused", reused!.Metadata.SnapshotState);

        ResetOperationalGovernanceDiagnosticsState();

        var fresh = await GetGovernanceSnapshotAsync();
        Assert.NotNull(fresh);
        Assert.Equal("Fresh", fresh!.Metadata.SnapshotState);
    }

    [SkippableFact]
    public async Task Runtime_baseline_collaborator_and_pipeline_counts_match_budget()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var dto = await GetRuntimeProtectionAsync();

        Assert.NotNull(dto);
        Assert.Equal(OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators, dto!.RuntimeBaseline.ProjectionCollaboratorCount);
        Assert.Equal(OperationalGovernanceProjectionPipeline.StageOrder.Count, dto.RuntimeBaseline.PipelineStageCount);
        Assert.True(dto.RuntimeBaseline.PipelineStageCount <= OperationalGovernanceComplexityMetrics.MaxPipelineStageCount);
    }

    private static void AssertNoDuplicateExplainabilityCodes(IReadOnlyList<string> codes)
    {
        var distinct = codes.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    private async Task<OperationalGovernanceRuntimeProtectionDto?> GetRuntimeProtectionAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/runtime-protection");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceRuntimeProtectionDto>();
    }

    private async Task<OperationalCacheGovernanceOverviewDto?> GetGovernanceOverviewAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();
    }

    private async Task<OperationalGovernanceFingerprintDto?> GetGovernanceFingerprintAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-fingerprint");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceFingerprintDto>();
    }

    private async Task<OperationalGovernanceSnapshotDto?> GetGovernanceSnapshotAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-snapshot");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceSnapshotDto>();
    }
}
