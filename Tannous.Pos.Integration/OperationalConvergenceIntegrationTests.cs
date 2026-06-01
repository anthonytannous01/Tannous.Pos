using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalConvergenceIntegrationTests : IntegrationTestBase
{
    private const string ConvergenceBase = "/api/v1.0/internal/operational-audit/convergence";
    private const string ConvergenceSummaryBase = "/api/v1.0/internal/operational-audit/convergence/summary";
    private const string DivergenceBase = "/api/v1.0/internal/operational-audit/convergence/divergence";
    private const string DigestBase = "/api/v1.0/internal/operational-audit/digest";
    private const string TopologyBase = "/api/v1.0/internal/operational-audit/topology";

    public OperationalConvergenceIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Convergence_report_returns_bounded_signal_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        await _client.GetAsync(DigestBase);
        await _client.GetAsync(TopologyBase);

        var response = await _client.GetAsync(ConvergenceBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalConvergenceReportDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Reinforcements.Count <= OperationalConvergenceAggregation.MaxReinforcements);
        Assert.True(dto.Ambiguities.Count <= OperationalConvergenceAggregation.MaxAmbiguities);
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.ConvergenceContinuity.OperatorInterpretation));
    }

    [SkippableFact]
    public async Task Convergence_summary_returns_stability_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ConvergenceSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalConvergenceSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantConvergenceArea));
        Assert.False(string.IsNullOrWhiteSpace(dto.StrongestReinforcement));
    }

    [SkippableFact]
    public async Task Operational_divergence_returns_deterministic_interpretation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(DivergenceBase);
        response.EnsureSuccessStatusCode();

        var divergences = await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalDivergenceDto>>();
        Assert.NotNull(divergences);
        Assert.True(divergences!.Count <= OperationalConvergenceAggregation.MaxDivergences);
    }

    [SkippableFact]
    public async Task Convergence_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetConvergenceReportAsync();
        var second = await GetConvergenceReportAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ConvergenceStrength, second!.ConvergenceStrength);
        Assert.Equal(
            first.Reinforcements.Select(r => r.OperationalArea),
            second.Reinforcements.Select(r => r.OperationalArea));
    }

    [SkippableFact]
    public async Task Convergence_continuity_consistent_with_prior_snapshots()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < 2; i++)
            await _client.GetAsync(ConvergenceBase);

        var report = await GetConvergenceReportAsync();
        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report!.ConvergenceContinuity.ReinforcementStability));
    }

    [SkippableFact]
    public async Task Convergence_avoids_probabilistic_and_ml_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var report = await GetConvergenceReportAsync();
        var summary = await GetConvergenceSummaryAsync();
        Assert.NotNull(report);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "Probabilistic", "Statistical", "Forecast" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Convergence_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalConvergenceAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(ConvergenceBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalConvergenceSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalConvergenceAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalConvergenceReportDto?> GetConvergenceReportAsync()
    {
        var response = await _client.GetAsync(ConvergenceBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalConvergenceReportDto>();
    }

    private async Task<OperationalConvergenceSummaryDto?> GetConvergenceSummaryAsync()
    {
        var response = await _client.GetAsync(ConvergenceSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalConvergenceSummaryDto>();
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
    }
}
