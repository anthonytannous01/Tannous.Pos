using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalPlaybookIntegrationTests : IntegrationTestBase
{
    private const string PlaybooksBase = "/api/v1.0/internal/operational-audit/playbooks";
    private const string PlaybooksSummaryBase = "/api/v1.0/internal/operational-audit/playbooks/summary";
    private const string StabilizationGuidanceBase = "/api/v1.0/internal/operational-audit/playbooks/stabilization-guidance";

    public OperationalPlaybookIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Playbooks_returns_bounded_operational_guidance()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(PlaybooksBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalPlaybooksDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Playbooks.Count <= OperationalPlaybookAggregation.MaxPlaybooks);
        Assert.True(dto.ResponseSteps.Count <= OperationalPlaybookAggregation.MaxResponseSteps);
        Assert.True(dto.EscalationGuidance.Count <= OperationalPlaybookAggregation.MaxEscalationGuidance);
        Assert.False(string.IsNullOrWhiteSpace(dto.ResponseAlignment.OperationalConsistency));
    }

    [SkippableFact]
    public async Task Playbook_summary_returns_priority_and_constraint()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(PlaybooksSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalPlaybookSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestPriorityArea));
    }

    [SkippableFact]
    public async Task Stabilization_guidance_returns_recovery_order()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(StabilizationGuidanceBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalStabilizationGuidanceDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.DominantConstraint));
        Assert.NotEmpty(dto.RecommendedRecoveryOrder);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorPriority));
    }

    [SkippableFact]
    public async Task Playbooks_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetPlaybooksAsync();
        var second = await GetPlaybooksAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Playbooks.Select(p => p.PlaybookId), second!.Playbooks.Select(p => p.PlaybookId));
    }

    [SkippableFact]
    public async Task Playbooks_avoid_governance_and_automation_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var playbooks = await GetPlaybooksAsync();
        var summary = await GetPlaybookSummaryAsync();
        Assert.NotNull(playbooks);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Workflow", "Automation", "Runbook" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(playbooks!.Playbooks, p => Assert.DoesNotContain(term, p.OperatorSummary, StringComparison.OrdinalIgnoreCase));
        }
    }

    [SkippableFact]
    public async Task Playbook_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalPlaybookAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(PlaybooksBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalPlaybookSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalPlaybookAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalPlaybooksDto?> GetPlaybooksAsync()
    {
        var response = await _client.GetAsync(PlaybooksBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPlaybooksDto>();
    }

    private async Task<OperationalPlaybookSummaryDto?> GetPlaybookSummaryAsync()
    {
        var response = await _client.GetAsync(PlaybooksSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPlaybookSummaryDto>();
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalCausalitySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalSituationSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalSimulationSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalPlaybookSnapshotStore>().Clear();
    }
}
