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
using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalStrategyIntegrationTests : IntegrationTestBase
{
    private const string StrategyReportBase = "/api/v1.0/internal/operational-audit/strategy";
    private const string StrategySummaryBase = "/api/v1.0/internal/operational-audit/strategy/summary";
    private const string CoordinationBase = "/api/v1.0/internal/operational-audit/strategy/coordination";
    private const string AttentionBase = "/api/v1.0/internal/operational-audit/attention";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";

    public OperationalStrategyIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Strategy_report_returns_bounded_posture_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(AttentionBase);
        await _client.GetAsync(ResilienceBase);

        var response = await _client.GetAsync(StrategyReportBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalStrategyReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.StrategicPostures.Count <= OperationalStrategyAggregation.MaxPostures);
        Assert.True(dto.OperationalCoordination.Count <= OperationalStrategyAggregation.MaxCoordination);
        Assert.True(dto.StrategicAlignments.Count <= OperationalStrategyAggregation.MaxAlignments);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrategyContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Strategy_summary_returns_strategic_coherence_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(StrategySummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalStrategySummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrongestOperationalAlignment));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantStrategicPressure));
    }

    [SkippableFact]
    public async Task Operational_coordination_returns_deterministic_structure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(CoordinationBase);
        response.EnsureSuccessStatusCode();

        var coordination = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalCoordinationDto>>();
        Assert.NotNull(coordination);
        Assert.True(coordination!.Count <= OperationalStrategyAggregation.MaxCoordination);
    }

    [SkippableFact]
    public async Task Strategy_posture_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetStrategyReportAsync();
        var second = await GetStrategyReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DominantOperationalPosture, second!.DominantOperationalPosture);
    }

    [SkippableFact]
    public async Task Strategy_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(StrategyReportBase);

        var report = await GetStrategyReportAsync();
        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report!.StrategyContinuity.CoordinationConsistency));
    }

    [SkippableFact]
    public async Task Strategy_avoids_bi_and_probabilistic_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetStrategyReportAsync();
        var summary = await GetStrategySummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "BusinessIntelligence", "Probabilistic", "MachineLearning", "Executive", "Optimization" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Strategy_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalStrategyAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(StrategyReportBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalStrategySnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalStrategyAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalStrategyReportDto?> GetStrategyReportAsync()
    {
        var response = await _client.GetAsync(StrategyReportBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalStrategyReportDto>();
    }

    private async Task<OperationalStrategySummaryDto?> GetStrategySummaryAsync()
    {
        var response = await _client.GetAsync(StrategySummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalStrategySummaryDto>();
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
        scope.ServiceProvider.GetRequiredService<IOperationalStrategySnapshotStore>().Clear();
    }
}
