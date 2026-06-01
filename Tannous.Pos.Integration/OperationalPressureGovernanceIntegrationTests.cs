using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalPressureGovernanceIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";

    public OperationalPressureGovernanceIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Pressure_lifecycle_reports_state_after_clamp()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        SeedStickyPressureFlags();

        var lifecycle = await GetPressureLifecycleAsync();
        Assert.NotNull(lifecycle);
        Assert.NotEmpty(lifecycle!.LifecycleState);
    }

    [SkippableFact]
    public async Task Sticky_pressure_recovers_after_governance_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        SeedStickyPressureFlags();
        Assert.True(GetPressureState().QueryDateRangeClamped);

        ResetOperationalGovernancePressureState(clearDiagnosticsCaches: true);

        var pressure = GetPressureState();
        Assert.False(pressure.QueryDateRangeClamped);
        Assert.False(pressure.QueryPageSizeClamped);
        Assert.False(pressure.ForensicExportTruncated);

        var recovery = await GetPressureRecoveryAsync();
        Assert.NotNull(recovery);
        Assert.True(recovery!.PressureFlagsCleared);
    }

    [SkippableFact]
    public async Task Convergence_improves_after_reset_despite_prior_degradation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        SeedStickyPressureFlags();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var beforeReset = await GetPressureConvergenceAsync();
        ResetOperationalGovernanceStabilization();
        var afterReset = await GetPressureConvergenceAsync();

        Assert.NotNull(beforeReset);
        Assert.NotNull(afterReset);
        Assert.True(afterReset!.ConvergenceScore >= beforeReset!.ConvergenceScore);
    }

    [SkippableFact]
    public async Task Repeated_recovery_cycles_are_deterministic_after_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetPressureConvergenceAsync();
        ResetOperationalGovernanceStabilization();
        var second = await GetPressureConvergenceAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ConvergenceClassification, second!.ConvergenceClassification);
        Assert.Equal(first.ConvergenceScore, second.ConvergenceScore);
    }

    [SkippableFact]
    public async Task Readiness_not_stuck_pressure_degraded_after_full_stabilization_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        SeedStickyPressureFlags();
        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        ResetOperationalGovernanceStabilization();

        var adaptive = await _client.GetAsync($"{CacheBase}/adaptive-summary");
        adaptive.EnsureSuccessStatusCode();
        var summary = await adaptive.Content.ReadFromJsonAsync<OperationalCacheAdaptiveSummaryDto>();
        Assert.NotNull(summary);
        Assert.NotEqual(OperationalCacheReadinessState.PressureDegraded, summary!.ReadinessState);
    }

    [SkippableFact]
    public async Task Reset_coordinator_is_idempotent()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        ResetOperationalGovernancePressureState();
        ResetOperationalGovernancePressureState();

        var pressure = GetPressureState();
        Assert.False(pressure.QueryDateRangeClamped);

        var lifecycle = await GetPressureLifecycleAsync();
        Assert.NotNull(lifecycle);
    }

    private void SeedStickyPressureFlags()
    {
        using var scope = _factory.Services.CreateScope();
        var resilience = scope.ServiceProvider.GetRequiredService<IOperationalResilienceDiagnosticsService>();
        resilience.NoteQueryPressure(dateRangeClamped: true, pageSizeClamped: true);
    }

    private IOperationalResiliencePressureState GetPressureState()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalResiliencePressureState>();
    }

    private async Task<OperationalPressureLifecycleDto?> GetPressureLifecycleAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/pressure-lifecycle");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPressureLifecycleDto>();
    }

    private async Task<OperationalPressureRecoveryDto?> GetPressureRecoveryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/pressure-recovery");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPressureRecoveryDto>();
    }

    private async Task<OperationalPressureConvergenceDto?> GetPressureConvergenceAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/pressure-convergence");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPressureConvergenceDto>();
    }
}
