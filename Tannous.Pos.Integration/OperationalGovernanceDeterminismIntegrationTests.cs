using System.Net.Http.Json;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceDeterminismIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceDeterminismIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Runtime_protection_reports_determinism_and_freeze_advisory_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var dto = await GetRuntimeProtectionAsync();

        Assert.NotNull(dto);
        Assert.Contains("DeterminismAuditPass", dto!.RuntimeBaseline.BaselineSignals);
        Assert.Contains("RuntimeConsistencyPass", dto.RuntimeBaseline.BaselineSignals);
        Assert.Contains("GovernanceFreezeCompliant", dto.RuntimeBaseline.BaselineSignals);
    }

    [SkippableFact]
    public async Task Explainability_and_recommendation_ordering_remain_stable()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetRuntimeProtectionAsync();
        var second = await GetRuntimeProtectionAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        AssertNoDuplicateExplainabilityCodes(first!.ExplainabilityCodes);
        Assert.Equal(first.ExplainabilityCodes, second!.ExplainabilityCodes);
        Assert.Equal(
            first.ProtectionRecommendations,
            second.ProtectionRecommendations);
    }

    private static void AssertNoDuplicateExplainabilityCodes(IReadOnlyList<string> codes)
    {
        var distinct = codes.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    [SkippableFact]
    public async Task Fingerprint_remains_stable_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var beforeReset = await GetGovernanceFingerprintAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var afterReset = await GetGovernanceFingerprintAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.Equal(beforeReset!.FingerprintHash, afterReset!.FingerprintHash);
    }

    [SkippableFact]
    public async Task Snapshot_is_fresh_after_governance_reset_not_reused()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        await GetGovernanceSnapshotAsync();
        ResetOperationalGovernanceDiagnosticsState();
        var snapshot = await GetGovernanceSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.Equal("Fresh", snapshot!.Metadata.SnapshotState);
    }

    [SkippableFact]
    public async Task Runtime_baseline_timing_band_is_deterministic_for_equivalent_state()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetRuntimeProtectionAsync();
        var second = await GetRuntimeProtectionAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(
            first!.RuntimeBaseline.ProjectionTiming.TimingBand,
            second!.RuntimeBaseline.ProjectionTiming.TimingBand);
        Assert.Equal(
            first.RuntimeBaseline.ExecutionBudgetState,
            second.RuntimeBaseline.ExecutionBudgetState);
    }

    private async Task<OperationalGovernanceRuntimeProtectionDto?> GetRuntimeProtectionAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/runtime-protection");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceRuntimeProtectionDto>();
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
