using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalAttentionIntegrationTests : IntegrationTestBase
{
    private const string AttentionReportBase = "/api/v1.0/internal/operational-audit/attention";
    private const string AttentionSummaryBase = "/api/v1.0/internal/operational-audit/attention/summary";
    private const string PrioritiesBase = "/api/v1.0/internal/operational-audit/attention/priorities";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string ConvergenceBase = "/api/v1.0/internal/operational-audit/convergence";

    public OperationalAttentionIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Attention_report_returns_bounded_priority_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(ConvergenceBase);
        await _client.GetAsync(ResilienceBase);

        var response = await _client.GetAsync(AttentionReportBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalAttentionReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Priorities.Count <= OperationalAttentionAggregation.MaxPriorities);
        Assert.True(dto.AttentionCoordination.Count <= OperationalAttentionAggregation.MaxCoordination);
        Assert.True(dto.OperationalEmphasis.Count <= OperationalAttentionAggregation.MaxEmphasis);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.AttentionContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Attention_summary_returns_focus_coordination_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(AttentionSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalAttentionSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.HighestPriorityConcern));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantStabilizationFocus));
    }

    [SkippableFact]
    public async Task Operational_priorities_returns_deterministic_structure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(PrioritiesBase);
        response.EnsureSuccessStatusCode();

        var priorities = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalPriorityDto>>();
        Assert.NotNull(priorities);
        Assert.True(priorities!.Count <= OperationalAttentionAggregation.MaxPriorities);
    }

    [SkippableFact]
    public async Task Attention_coordination_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetAttentionReportAsync();
        var second = await GetAttentionReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DominantOperationalPriority, second!.DominantOperationalPriority);
    }

    [SkippableFact]
    public async Task Attention_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(AttentionReportBase);

        var report = await GetAttentionReportAsync();
        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report!.AttentionContinuity.PriorityConsistency));
    }

    [SkippableFact]
    public async Task Attention_avoids_automation_and_probabilistic_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetAttentionReportAsync();
        var summary = await GetAttentionSummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Notification", "Alerting", "Probabilistic", "MachineLearning", "Workflow" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Attention_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalAttentionAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(AttentionReportBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalAttentionSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalAttentionAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalAttentionReportDto?> GetAttentionReportAsync()
    {
        var response = await _client.GetAsync(AttentionReportBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalAttentionReportDto>();
    }

    private async Task<OperationalAttentionSummaryDto?> GetAttentionSummaryAsync()
    {
        var response = await _client.GetAsync(AttentionSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalAttentionSummaryDto>();
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
        scope.ServiceProvider.GetRequiredService<IOperationalPatternSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIntegritySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalExperienceSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalDigestSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalEvolutionSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTopologySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalConvergenceSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalResilienceCognitionSnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalAttentionSnapshotStore>().Clear();
    }
}
