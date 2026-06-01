using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalPatternIntegrationTests : IntegrationTestBase
{
    private const string PatternsBase = "/api/v1.0/internal/operational-audit/patterns";
    private const string PatternsSummaryBase = "/api/v1.0/internal/operational-audit/patterns/summary";
    private const string ArchetypesBase = "/api/v1.0/internal/operational-audit/patterns/archetypes";

    public OperationalPatternIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Patterns_returns_bounded_operational_pattern_analysis()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(PatternsBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalPatternsDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Patterns.Count <= OperationalPatternAggregation.MaxPatterns);
        Assert.True(dto.Correlations.Count <= OperationalPatternAggregation.MaxCorrelations);
        Assert.True(dto.Sequences.Count <= OperationalPatternAggregation.MaxSequences);
        Assert.False(string.IsNullOrWhiteSpace(dto.Outlook.DominantPattern));
    }

    [SkippableFact]
    public async Task Pattern_summary_returns_recurring_and_archetype_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(PatternsSummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalPatternSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantArchetype));
    }

    [SkippableFact]
    public async Task Stabilization_archetypes_returns_bounded_recognition()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ArchetypesBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalStabilizationArchetypesDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Archetypes.Count <= OperationalPatternAggregation.MaxArchetypes);
        Assert.All(dto.Archetypes, a => Assert.False(string.IsNullOrWhiteSpace(a.OperatorInterpretation)));
    }

    [SkippableFact]
    public async Task Patterns_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetPatternsAsync();
        var second = await GetPatternsAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Patterns.Select(p => p.PatternId), second!.Patterns.Select(p => p.PatternId));
    }

    [SkippableFact]
    public async Task Patterns_avoid_governance_and_ml_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var patterns = await GetPatternsAsync();
        var summary = await GetPatternSummaryAsync();
        Assert.NotNull(patterns);
        Assert.NotNull(summary);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "Anomaly", "Clustering" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(patterns!.Patterns, p => Assert.DoesNotContain(term, p.OperatorSummary, StringComparison.OrdinalIgnoreCase));
        }
    }

    [SkippableFact]
    public async Task Pattern_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalPatternAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(PatternsBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalPatternSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalPatternAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalPatternsDto?> GetPatternsAsync()
    {
        var response = await _client.GetAsync(PatternsBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPatternsDto>();
    }

    private async Task<OperationalPatternSummaryDto?> GetPatternSummaryAsync()
    {
        var response = await _client.GetAsync(PatternsSummaryBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalPatternSummaryDto>();
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
    }
}
