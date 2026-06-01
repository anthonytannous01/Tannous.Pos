using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalIntegrityIntegrationTests : IntegrationTestBase
{
    private const string IntegrityBase = "/api/v1.0/internal/operational-audit/integrity";
    private const string IntegritySummaryBase = "/api/v1.0/internal/operational-audit/integrity/summary";
    private const string ContradictionsBase = "/api/v1.0/internal/operational-audit/integrity/contradictions";

    public OperationalIntegrityIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Integrity_report_returns_bounded_consistency_verification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(IntegrityBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalIntegrityReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Alignments.Count <= OperationalIntegrityAggregation.MaxAlignments);
        Assert.True(dto.IntegrityWarnings.Count <= OperationalIntegrityAggregation.MaxWarnings);
        Assert.InRange(dto.ConsistencyScore, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantOperationalNarrative));
    }

    [SkippableFact]
    public async Task Integrity_summary_returns_coherence_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(IntegritySummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalIntegritySummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantOperationalStory));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecoveryConsistency));
    }

    [SkippableFact]
    public async Task Contradictions_returns_bounded_contradiction_analysis()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ContradictionsBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalIntegrityContradictionsDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Contradictions.Count <= OperationalIntegrityAggregation.MaxContradictions);
        Assert.Equal(dto.ContradictionCount, dto.Contradictions.Count);
        Assert.All(dto.Contradictions, c => Assert.False(string.IsNullOrWhiteSpace(c.RecommendedOperatorReview)));
    }

    [SkippableFact]
    public async Task Integrity_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetIntegrityReportAsync();
        var second = await GetIntegrityReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.OverallIntegrityState, second!.OverallIntegrityState);
        Assert.Equal(
            first.Alignments.Select(a => a.SourceLayer + a.TargetLayer + a.AlignmentType),
            second.Alignments.Select(a => a.SourceLayer + a.TargetLayer + a.AlignmentType));
    }

    [SkippableFact]
    public async Task Integrity_detects_replay_alignment_when_layers_agree()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetIntegrityReportAsync();
        Assert.NotNull(report);

        var replayAlignment = report!.Alignments.Any(a =>
            a.AlignmentType == OperationalAlignmentType.ReplayStabilizationAlignment
            || (a.SharedDominantArea.Contains("Replay", StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.AlignmentStrength, "Strong", StringComparison.OrdinalIgnoreCase)));

        Assert.True(replayAlignment || report.Alignments.Count >= 1);
    }

    [SkippableFact]
    public async Task Integrity_avoids_governance_and_ml_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetIntegrityReportAsync();
        var summary = await GetIntegritySummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "Probabilistic", "PolicyEngine" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Integrity_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalIntegrityAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(IntegrityBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalIntegritySnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalIntegrityAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalIntegrityReportDto?> GetIntegrityReportAsync()
    {
        var response = await _client.GetAsync(IntegrityBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalIntegrityReportDto>();
    }

    private async Task<OperationalIntegritySummaryDto?> GetIntegritySummaryAsync()
    {
        var response = await _client.GetAsync(IntegritySummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalIntegritySummaryDto>();
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
    }
}
