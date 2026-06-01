using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceFingerprintingIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceFingerprintingIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Identical_conditions_produce_identical_fingerprints_within_reuse_window()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetGovernanceFingerprintAsync();
        var second = await GetGovernanceFingerprintAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.False(string.IsNullOrWhiteSpace(first!.FingerprintHash));
        Assert.Equal(first.FingerprintHash, second!.FingerprintHash);
        Assert.Equal(first.Signature.NormalizedSignature, second.Signature.NormalizedSignature);
        AssertNoDuplicateExplainabilityCodes(first.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Stable_fingerprint_increments_stable_hit_telemetry_after_rebuild_with_same_state()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        await GetGovernanceFingerprintAsync();
        await Task.Delay(TimeSpan.FromSeconds(OperationalGovernanceSnapshotReuseConstants.TtlSeconds + 1));
        var telemetryBefore = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();
        await GetGovernanceFingerprintAsync();
        var telemetryAfter = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();

        Assert.True(telemetryAfter.GovernanceStableFingerprintHits > telemetryBefore.GovernanceStableFingerprintHits);
    }

    [SkippableFact]
    public async Task Replay_consistency_endpoint_increments_checks_and_returns_bounded_shape()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var telemetryBefore = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();
        var dto = await GetReplayConsistencyAsync();
        var telemetryAfter = GetOperationalDiagnosticsCacheTelemetry().GetSnapshot();

        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.ReplayConsistencyLevel));
        Assert.False(string.IsNullOrWhiteSpace(dto.FingerprintHash));
        Assert.True(dto.ReplayConsistencyChecks > telemetryBefore.ReplayConsistencyChecks);
        Assert.True(dto.ExplainabilityCodes.Count <= OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);
        AssertNoDuplicateExplainabilityCodes(dto.ExplainabilityCodes);
        Assert.True(telemetryAfter.ReplayConsistencyChecks >= telemetryBefore.ReplayConsistencyChecks);
    }

    [SkippableFact]
    public async Task Drift_analysis_aligns_with_fingerprint_after_snapshot_rebuild()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        await GetGovernanceFingerprintAsync();
        await Task.Delay(TimeSpan.FromSeconds(OperationalGovernanceSnapshotReuseConstants.TtlSeconds + 1));
        var fingerprint = await GetGovernanceFingerprintAsync();
        var drift = await GetGovernanceDriftAnalysisAsync();

        Assert.NotNull(fingerprint);
        Assert.NotNull(drift);
        Assert.Equal(fingerprint!.FingerprintHash, drift!.FingerprintHash);
        Assert.Equal(fingerprint.FingerprintStability, drift.FingerprintStability);
        AssertNoDuplicateExplainabilityCodes(drift.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Explainability_ordering_is_stable_on_fingerprint_endpoint()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var dto = await GetGovernanceFingerprintAsync();

        Assert.NotNull(dto);
        AssertNoDuplicateExplainabilityCodes(dto!.ExplainabilityCodes);
        Assert.Equal(
            dto.ExplainabilityCodes.OrderBy(s => s, StringComparer.Ordinal),
            dto.ExplainabilityCodes);
    }

    private static void AssertNoDuplicateExplainabilityCodes(IReadOnlyList<string> codes)
    {
        var distinct = codes.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    private async Task<OperationalGovernanceFingerprintDto?> GetGovernanceFingerprintAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-fingerprint");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceFingerprintDto>();
    }

    private async Task<OperationalGovernanceDriftAnalysisDto?> GetGovernanceDriftAnalysisAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-drift-analysis");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceDriftAnalysisDto>();
    }

    private async Task<OperationalGovernanceReplayConsistencyDto?> GetReplayConsistencyAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/replay-consistency");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceReplayConsistencyDto>();
    }
}
