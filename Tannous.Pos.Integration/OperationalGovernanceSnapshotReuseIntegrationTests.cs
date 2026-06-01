using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceSnapshotReuseIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceSnapshotReuseIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Governance_snapshot_endpoint_returns_bounded_projection_shape()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var dto = await GetGovernanceSnapshotAsync();

        Assert.NotNull(dto);
        Assert.NotNull(dto!.Metadata);
        Assert.NotNull(dto.Freshness);
        Assert.NotNull(dto.Overview);
        Assert.NotNull(dto.RuntimeProtection);
        Assert.True(dto.ExplainabilityCodes.Count <= OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);
        AssertNoDuplicateExplainabilityCodes(dto.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Reuse_within_ttl_increments_reuse_counters_and_preserves_overview()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var telemetryBefore = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();
        var firstOverview = await GetGovernanceOverviewAsync();
        var secondOverview = await GetGovernanceOverviewAsync();
        var telemetryAfter = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();

        Assert.NotNull(firstOverview);
        Assert.NotNull(secondOverview);
        Assert.Equal(firstOverview!.ReadinessState, secondOverview!.ReadinessState);
        Assert.Equal(firstOverview.PressureSeverity, secondOverview.PressureSeverity);
        Assert.True(telemetryAfter.GovernanceSnapshotReuses > telemetryBefore.GovernanceSnapshotReuses);
        Assert.True(telemetryAfter.ProjectionReuseHits > telemetryBefore.ProjectionReuseHits);
    }

    [SkippableFact]
    public async Task Projection_reuse_and_consistency_endpoints_align_with_snapshot_state()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        await GetGovernanceSnapshotAsync();
        var snapshot = await GetGovernanceSnapshotAsync();
        var reuse = await GetProjectionReuseAsync();
        var consistency = await GetProjectionConsistencyAsync();

        Assert.NotNull(snapshot);
        Assert.NotNull(reuse);
        Assert.NotNull(consistency);
        Assert.Equal("Reused", snapshot!.Metadata.SnapshotState);
        Assert.Equal("Reused", reuse!.SnapshotState);
        Assert.Equal(reuse.SnapshotState, consistency!.SnapshotState);
        AssertNoDuplicateExplainabilityCodes(snapshot.ExplainabilityCodes);
        AssertNoDuplicateExplainabilityCodes(consistency.ConsistencySignals);
    }

    [SkippableFact]
    public async Task Snapshot_rebuilds_after_ttl_expiry()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetGovernanceSnapshotAsync();
        await Task.Delay(TimeSpan.FromSeconds(OperationalGovernanceSnapshotReuseConstants.TtlSeconds + 1));
        var telemetryBefore = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();
        var second = await GetGovernanceSnapshotAsync();
        var telemetryAfter = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(telemetryAfter.GovernanceSnapshotBuilds > telemetryBefore.GovernanceSnapshotBuilds);
        Assert.True(telemetryAfter.ProjectionReuseMisses >= telemetryBefore.ProjectionReuseMisses);
    }

    [SkippableFact]
    public async Task Explainability_ordering_is_stable_across_repeated_snapshot_queries()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var fresh = await GetGovernanceSnapshotAsync();

        Assert.NotNull(fresh);
        Assert.Equal("Fresh", fresh!.Metadata.SnapshotState);
        AssertNoDuplicateExplainabilityCodes(fresh.ExplainabilityCodes);
        Assert.Equal(
            fresh.ExplainabilityCodes.OrderBy(s => s, StringComparer.Ordinal),
            fresh.ExplainabilityCodes);

        var reused = await GetGovernanceSnapshotAsync();

        Assert.NotNull(reused);
        Assert.Equal(reused!.Overview.ReadinessState, fresh.Overview.ReadinessState);
        Assert.Equal(reused.Overview.PressureSeverity, fresh.Overview.PressureSeverity);
        AssertNoDuplicateExplainabilityCodes(reused.ExplainabilityCodes);
        Assert.Contains("SnapshotReused", reused.ExplainabilityCodes);
    }

    private static void AssertNoDuplicateExplainabilityCodes(IReadOnlyList<string> codes)
    {
        var distinct = codes.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    private async Task<OperationalGovernanceSnapshotDto?> GetGovernanceSnapshotAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-snapshot");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceSnapshotDto>();
    }

    private async Task<OperationalGovernanceProjectionReuseDto?> GetProjectionReuseAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/projection-reuse");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceProjectionReuseDto>();
    }

    private async Task<OperationalGovernanceProjectionConsistencyDto?> GetProjectionConsistencyAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/projection-consistency");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceProjectionConsistencyDto>();
    }

    private async Task<OperationalCacheGovernanceOverviewDto?> GetGovernanceOverviewAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();
    }
}
