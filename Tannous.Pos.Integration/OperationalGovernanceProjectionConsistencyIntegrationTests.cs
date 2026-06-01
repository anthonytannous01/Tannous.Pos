using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceProjectionConsistencyIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceProjectionConsistencyIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Governance_audit_explainability_is_deterministic_across_repeated_calls()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetGovernanceAuditAsync();
        var second = await GetGovernanceAuditAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ReasonCodes, second!.ReasonCodes);
        Assert.Equal(first.TriggerSignals, second.TriggerSignals);
        AssertNoDuplicateExplainabilityCodes(first.ReasonCodes);
    }

    [SkippableFact]
    public async Task Consistency_recovery_projection_remains_internally_consistent_after_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        SeedStickyPressure();
        ResetOperationalGovernanceStabilization();

        var recovery = await GetConsistencyRecoveryAsync();
        var confidence = await GetConsistencyConfidenceAsync();

        Assert.NotNull(recovery);
        Assert.NotNull(confidence);
        AssertNoDuplicateExplainabilityCodes(recovery!.ReasonCodes);
        AssertNoDuplicateExplainabilityCodes(confidence!.ReasonCodes);
        Assert.True(recovery.ConfidenceLevel == confidence.ConfidenceLevel);
    }

    [SkippableFact]
    public async Task Pressure_convergence_explainability_is_stable_after_stabilization_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        SeedStickyPressure();
        (await _client.GetAsync($"{CacheBase}/governance-overview")).EnsureSuccessStatusCode();

        ResetOperationalGovernanceStabilization();

        var first = await GetPressureConvergenceAsync();
        var second = await GetPressureConvergenceAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ReasonCodes, second!.ReasonCodes);
        AssertNoDuplicateExplainabilityCodes(first.ReasonCodes);
    }

    [SkippableFact]
    public async Task Invalidation_audit_bounded_signals_remain_ordered()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var audit = await GetInvalidationAuditAsync();
        Assert.NotNull(audit);
        AssertNoDuplicateExplainabilityCodes(audit!.ReasonCodes);
        Assert.True(audit.ReasonCodes.Count <= OperationalGovernanceSurfaceBudget.MaxExplainabilityItemsPerProjection);
        Assert.All(audit.ReasonCodes, code => Assert.True(code.Length <= OperationalGovernanceSurfaceBudget.MaxExplainabilityCodeLength));
        Assert.Equal(audit.ReasonCodes.OrderBy(c => c, StringComparer.Ordinal).ToList(), audit.ReasonCodes.ToList());
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

    private async Task<OperationalCacheGovernanceAuditDto?> GetGovernanceAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceAuditDto>();
    }

    private async Task<OperationalCacheConsistencyRecoveryDto?> GetConsistencyRecoveryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/consistency-recovery");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyRecoveryDto>();
    }

    private async Task<OperationalCacheConsistencyConfidenceDto?> GetConsistencyConfidenceAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/consistency-confidence");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyConfidenceDto>();
    }

    private async Task<OperationalPressureConvergenceDto?> GetPressureConvergenceAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/pressure-convergence");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPressureConvergenceDto>();
    }

    private async Task<OperationalCacheInvalidationAuditDto?> GetInvalidationAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/invalidation-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationAuditDto>();
    }
}
