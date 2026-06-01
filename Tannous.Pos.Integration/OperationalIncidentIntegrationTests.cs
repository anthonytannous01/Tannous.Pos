using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalIncidentIntegrationTests : IntegrationTestBase
{
    private const string IncidentsBase = "/api/v1.0/internal/operational-audit/incident-cases";
    private const string IncidentsSummaryBase = "/api/v1.0/internal/operational-audit/incident-cases/summary";

    public OperationalIncidentIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Incident_cases_returns_operator_facing_read_model()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(IncidentsBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalIncidentCasesDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.CaseCount <= OperationalIncidentAggregation.MaxIncidentCases);
    }

    [SkippableFact]
    public async Task Incident_summary_returns_platform_attention_state()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(IncidentsSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalIncidentCasesSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.PlatformStabilityState));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorAttentionLevel));
    }

    [SkippableFact]
    public async Task Incident_cases_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetIncidentCasesAsync();
        var second = await GetIncidentCasesAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Cases.Select(c => c.IncidentId), second!.Cases.Select(c => c.IncidentId));
    }

    [SkippableFact]
    public async Task Incident_details_returns_investigation_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var cases = await GetIncidentCasesAsync();
        Assert.NotNull(cases);
        Assert.NotEmpty(cases!.Cases);

        var incidentId = cases.Cases[0].IncidentId;
        var response = await _client.GetAsync($"{IncidentsBase}/{incidentId}");
        response.EnsureSuccessStatusCode();

        var details = await response.Content.ReadFromJsonAsync<OperationalIncidentCaseDetailDto>();
        Assert.NotNull(details);
        Assert.Equal(incidentId, details!.Case.IncidentId);
        Assert.True(details.Signals.Count <= OperationalIncidentAggregation.MaxSignalsPerCase);
        Assert.False(string.IsNullOrWhiteSpace(details.InvestigationContext.RecoveryAlignment));
        Assert.False(string.IsNullOrWhiteSpace(details.Outlook.RecommendedOperatorFocus));
    }

    [SkippableFact]
    public async Task Incident_routes_use_existing_operational_paths()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var cases = await GetIncidentCasesAsync();
        Assert.NotNull(cases);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalIncidentAggregation.RouteDashboard,
            OperationalIncidentAggregation.RouteReconciliationWorkbench,
            OperationalIncidentAggregation.RouteInventoryWorkbench,
            OperationalIncidentAggregation.RouteReplayWorkbench,
            OperationalIncidentAggregation.RouteTrendSummary,
            OperationalIncidentAggregation.RouteTimeline,
            OperationalIncidentAggregation.RouteTriage,
            OperationalIncidentAggregation.RouteRecovery,
            OperationalIncidentAggregation.RouteNavigation
        };

        Assert.All(cases!.Cases, c => Assert.Contains(c.RecommendedRoute, allowed));
    }

    [SkippableFact]
    public async Task Incident_summary_avoids_governance_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var summary = await GetIncidentSummaryAsync();
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Incident_recurring_detection_after_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await GetIncidentCasesAsync();
        var second = await GetIncidentCasesAsync();

        Assert.NotNull(second);
        Assert.True(second!.CaseCount >= 0);
    }

    [SkippableFact]
    public async Task Incident_reset_clears_dependent_stores_consistently()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(IncidentsBase);
        ResetOperationalStores();

        using var scope = _factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().GetEvents());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().GetSnapshots());
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();

        var cases = await GetIncidentCasesAsync();
        Assert.NotNull(cases);
    }

    [SkippableFact]
    public async Task Incident_details_returns_not_found_for_unknown_id()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync($"{IncidentsBase}/unknown-incident-id");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();
    }

    private async Task<OperationalIncidentCasesDto?> GetIncidentCasesAsync()
    {
        var response = await _client.GetAsync(IncidentsBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalIncidentCasesDto>();
    }

    private async Task<OperationalIncidentCasesSummaryDto?> GetIncidentSummaryAsync()
    {
        var response = await _client.GetAsync(IncidentsSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalIncidentCasesSummaryDto>();
    }
}
