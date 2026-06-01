using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalRecoveryIntegrationTests : IntegrationTestBase
{
    private const string RecoveryBase = "/api/v1.0/internal/operational-audit/recovery";
    private const string RecoveryOutlookBase = "/api/v1.0/internal/operational-audit/recovery/outlook";

    public OperationalRecoveryIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Recovery_posture_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(RecoveryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalRecoveryPostureDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.True(dto.Signals.Count <= OperationalRecoveryAggregation.MaxSignals);
        Assert.True(dto.Recommendations.Count <= OperationalRecoveryAggregation.MaxRecommendations);
        Assert.True(dto.Attention.Count <= OperationalRecoveryAggregation.MaxAttentionItems);
        Assert.True(dto.Convergence.Count <= OperationalRecoveryAggregation.MaxConvergenceItems);
    }

    [SkippableFact]
    public async Task Recovery_outlook_returns_five_sections()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(RecoveryOutlookBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalRecoveryOutlookDto>();
        Assert.NotNull(dto);
        Assert.Equal(5, dto!.SectionCount);
        Assert.False(string.IsNullOrWhiteSpace(dto.Summary));
        Assert.Contains(dto.Sections, s => s.SectionId == OperationalRecoveryAggregation.SectionReplayRecovery);
        Assert.Contains(dto.Sections, s => s.SectionId == OperationalRecoveryAggregation.SectionInventoryStabilization);
    }

    [SkippableFact]
    public async Task Recovery_posture_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetRecoveryPostureAsync();
        var second = await GetRecoveryPostureAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.OverallState, second!.OverallState);
        Assert.Equal(first.OverallDirection, second.OverallDirection);
        Assert.Equal(first.Signals.Select(s => s.SignalId), second.Signals.Select(s => s.SignalId));
    }

    [SkippableFact]
    public async Task Recovery_routes_use_existing_operational_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var posture = await GetRecoveryPostureAsync();
        var outlook = await GetRecoveryOutlookAsync();
        Assert.NotNull(posture);
        Assert.NotNull(outlook);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalRecoveryAggregation.RouteDashboard,
            OperationalRecoveryAggregation.RouteReconciliationWorkbench,
            OperationalRecoveryAggregation.RouteInventoryWorkbench,
            OperationalRecoveryAggregation.RouteReplayWorkbench,
            OperationalRecoveryAggregation.RouteTrendSummary,
            OperationalRecoveryAggregation.RouteTimeline,
            OperationalRecoveryAggregation.RouteTriage,
            OperationalRecoveryAggregation.RouteNavigation
        };

        Assert.All(posture!.Signals, s => Assert.Contains(s.RecommendedRoute, allowed));
        Assert.All(posture.Recommendations, r => Assert.Contains(r.RecommendedRoute, allowed));
        Assert.All(outlook!.Sections, s => Assert.Contains(s.RecommendedRoute, allowed));
    }

    [SkippableFact]
    public async Task Recovery_summary_avoids_governance_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var posture = await GetRecoveryPostureAsync();
        var outlook = await GetRecoveryOutlookAsync();
        Assert.NotNull(posture);
        Assert.NotNull(outlook);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, posture!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, outlook!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Recovery_reset_clears_dependent_stores_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(RecoveryBase);
        ResetOperationalStores();

        using var scope = _factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().GetEvents());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().GetSnapshots());

        var posture = await GetRecoveryPostureAsync();
        Assert.NotNull(posture);
        Assert.NotEmpty(posture!.Signals);
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
    }

    private async Task<OperationalRecoveryPostureDto?> GetRecoveryPostureAsync()
    {
        var response = await _client.GetAsync(RecoveryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalRecoveryPostureDto>();
    }

    private async Task<OperationalRecoveryOutlookDto?> GetRecoveryOutlookAsync()
    {
        var response = await _client.GetAsync(RecoveryOutlookBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalRecoveryOutlookDto>();
    }
}
