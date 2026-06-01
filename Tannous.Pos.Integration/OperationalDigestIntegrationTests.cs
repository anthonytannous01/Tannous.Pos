using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalDigestIntegrationTests : IntegrationTestBase
{
    private const string DigestBase = "/api/v1.0/internal/operational-audit/digest";
    private const string ExecutiveBase = "/api/v1.0/internal/operational-audit/digest/executive";
    private const string SummaryBase = "/api/v1.0/internal/operational-audit/digest/summary";

    public OperationalDigestIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Operational_digest_returns_bounded_condensed_intelligence()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(DigestBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalDigestDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.OperationalHighlights.Count <= OperationalDigestAggregation.MaxHighlights);
        Assert.True(dto.NavigationHighlights.Count <= OperationalDigestAggregation.MaxNavigationHighlights);
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantOperationalStory));
        Assert.False(string.IsNullOrWhiteSpace(dto.ExecutiveDigest));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperatorDigest));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecommendedOperatorFocus));
    }

    [SkippableFact]
    public async Task Executive_digest_returns_condensed_leadership_summary()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(ExecutiveBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalExecutiveDigestDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Headline));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantNarrative));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecommendedPriority));
        Assert.True(dto.ExecutivePriorities.Count <= OperationalDigestAggregation.MaxExecutivePriorities);
    }

    [SkippableFact]
    public async Task Digest_summary_returns_operational_state_context()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalDigestSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.DominantNarrative));
        Assert.False(string.IsNullOrWhiteSpace(dto.IntegrityAlignment));
    }

    [SkippableFact]
    public async Task Digest_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetDigestAsync();
        var second = await GetDigestAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DigestState, second!.DigestState);
        Assert.Equal(
            first.OperationalHighlights.Select(h => h.Title),
            second.OperationalHighlights.Select(h => h.Title));
    }

    [SkippableFact]
    public async Task Digest_includes_navigation_highlights()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var digest = await GetDigestAsync();
        Assert.NotNull(digest);
        Assert.NotEmpty(digest!.NavigationHighlights);
        Assert.All(digest.NavigationHighlights, n => Assert.False(string.IsNullOrWhiteSpace(n.RecommendedSurface)));
        Assert.All(digest.NavigationHighlights, n => Assert.False(string.IsNullOrWhiteSpace(n.OperatorInterpretation)));
    }

    [SkippableFact]
    public async Task Digest_avoids_governance_and_ai_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var digest = await GetDigestAsync();
        var executive = await GetExecutiveDigestAsync();
        Assert.NotNull(digest);
        Assert.NotNull(executive);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "MachineLearning", "LLM", "Analytics" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, digest!.ExecutiveDigest, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, executive!.Headline, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task Digest_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalDigestAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(DigestBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalDigestSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalDigestAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalDigestDto?> GetDigestAsync()
    {
        var response = await _client.GetAsync(DigestBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalDigestDto>();
    }

    private async Task<OperationalExecutiveDigestDto?> GetExecutiveDigestAsync()
    {
        var response = await _client.GetAsync(ExecutiveBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalExecutiveDigestDto>();
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
    }
}
