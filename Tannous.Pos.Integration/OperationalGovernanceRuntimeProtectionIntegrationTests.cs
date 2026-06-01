using System.Net.Http.Json;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Integration;

public class OperationalGovernanceRuntimeProtectionIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";

    public OperationalGovernanceRuntimeProtectionIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Runtime_protection_endpoint_returns_bounded_projection_shape()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var dto = await GetRuntimeProtectionAsync();

        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.ExecutionState));
        Assert.False(string.IsNullOrWhiteSpace(dto.BudgetPressure));
        Assert.False(string.IsNullOrWhiteSpace(dto.ProjectionComplexity));
        Assert.False(string.IsNullOrWhiteSpace(dto.TelemetrySaturationLevel));
        Assert.NotNull(dto.Budget);
        Assert.NotNull(dto.ExecutionDiagnostics);
        Assert.NotNull(dto.TelemetrySaturation);
        Assert.NotNull(dto.Failsafe);
        Assert.True(dto.ExplainabilityCodes.Count <= OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);
        Assert.True(dto.ProtectionRecommendations.Count <= OperationalGovernanceRuntimeBudget.MaxGovernanceRecommendations);
        AssertNoDuplicateExplainabilityCodes(dto.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Runtime_protection_outputs_are_deterministic_across_repeated_queries()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var first = await GetRuntimeProtectionAsync();
        var second = await GetRuntimeProtectionAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ExecutionState, second!.ExecutionState);
        Assert.Equal(first.BudgetPressure, second.BudgetPressure);
        Assert.Equal(first.ProjectionComplexity, second.ProjectionComplexity);
        Assert.Equal(first.ExplainabilityCodes, second.ExplainabilityCodes);
    }

    [SkippableFact]
    public async Task Execution_diagnostics_and_telemetry_saturation_endpoints_align_with_runtime_state()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var runtime = await GetRuntimeProtectionAsync();
        var execution = await GetExecutionDiagnosticsAsync();
        var saturation = await GetTelemetrySaturationAsync();

        Assert.NotNull(runtime);
        Assert.NotNull(execution);
        Assert.NotNull(saturation);
        Assert.Equal(runtime!.ExecutionState, execution!.ExecutionState);
        Assert.Equal(runtime.BudgetPressure, execution.BudgetPressure);
        Assert.Equal(runtime.ProjectionComplexity, execution.ProjectionComplexity);
        Assert.Equal(runtime.TelemetrySaturationLevel, saturation!.SaturationLevel);
        AssertNoDuplicateExplainabilityCodes(execution.ReasonCodes);
        AssertNoDuplicateExplainabilityCodes(saturation.SaturationSignals);
    }

    [SkippableFact]
    public async Task Failsafe_state_is_visible_without_disabling_diagnostics()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalGovernanceStabilization();

        var protection = await GetRuntimeProtectionAsync();
        var summary = await GetGovernanceOverviewAsync();

        Assert.NotNull(protection);
        Assert.NotNull(summary);
        Assert.NotNull(protection!.Failsafe);
        Assert.True(Enum.IsDefined(summary!.ReadinessState));
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

    private async Task<OperationalGovernanceExecutionDiagnosticsDto?> GetExecutionDiagnosticsAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/execution-diagnostics");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceExecutionDiagnosticsDto>();
    }

    private async Task<OperationalGovernanceTelemetrySaturationDto?> GetTelemetrySaturationAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/telemetry-saturation");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalGovernanceTelemetrySaturationDto>();
    }

    private async Task<OperationalCacheGovernanceOverviewDto?> GetGovernanceOverviewAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/governance-overview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheGovernanceOverviewDto>();
    }
}
