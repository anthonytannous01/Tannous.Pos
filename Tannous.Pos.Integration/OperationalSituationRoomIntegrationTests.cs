using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Integration;

public class OperationalSituationRoomIntegrationTests : IntegrationTestBase
{
    private const string SituationRoomBase = "/api/v1.0/internal/operational-audit/situation-room";
    private const string BriefingBase = "/api/v1.0/internal/operational-audit/situation-room/briefing";
    private const string SummaryBase = "/api/v1.0/internal/operational-audit/situation-room/summary";

    public OperationalSituationRoomIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Situation_room_returns_operator_facing_briefing()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SituationRoomBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalSituationRoomDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.OperatorSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.ExecutiveSummary));
        Assert.False(string.IsNullOrWhiteSpace(dto.OperationalNarrative));
        Assert.True(dto.Narratives.Count <= OperationalSituationRoomAggregation.MaxNarratives);
        Assert.True(dto.RiskConcentrations.Count <= OperationalSituationRoomAggregation.MaxRiskConcentrations);
    }

    [SkippableFact]
    public async Task Executive_briefing_returns_concise_summary()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(BriefingBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalExecutiveBriefingDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Headline));
        Assert.False(string.IsNullOrWhiteSpace(dto.Summary));
        Assert.False(string.IsNullOrWhiteSpace(dto.RecommendedAction));
    }

    [SkippableFact]
    public async Task Situation_summary_returns_platform_posture()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var response = await _client.GetAsync(SummaryBase);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OperationalSituationSummaryDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.DominantArea));
    }

    [SkippableFact]
    public async Task Situation_room_stable_on_repeated_reads()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var first = await GetSituationRoomAsync();
        var second = await GetSituationRoomAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.DominantOperationalRisk, second!.DominantOperationalRisk);
        Assert.Equal(first.PlatformCondition, second.PlatformCondition);
    }

    [SkippableFact]
    public async Task Situation_room_avoids_governance_terminology()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        var room = await GetSituationRoomAsync();
        var briefing = await GetExecutiveBriefingAsync();
        Assert.NotNull(room);
        Assert.NotNull(briefing);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, room!.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, briefing!.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(room.Narratives, n => Assert.DoesNotContain(term, n.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [SkippableFact]
    public async Task Situation_room_snapshot_store_retains_bounded_history()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalStores();
        ResetOperationalGovernanceDiagnosticsState();

        for (var i = 0; i < OperationalSituationRoomAggregation.MaxStoredSnapshots + 2; i++)
            await _client.GetAsync(SituationRoomBase);

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationalSituationSnapshotStore>();
        Assert.True(store.GetSnapshots().Count <= OperationalSituationRoomAggregation.MaxStoredSnapshots);
    }

    private async Task<OperationalSituationRoomDto?> GetSituationRoomAsync()
    {
        var response = await _client.GetAsync(SituationRoomBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalSituationRoomDto>();
    }

    private async Task<OperationalExecutiveBriefingDto?> GetExecutiveBriefingAsync()
    {
        var response = await _client.GetAsync(BriefingBase);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalExecutiveBriefingDto>();
    }

    private void ResetOperationalStores()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalTrendWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalTimelineWindowStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalIncidentCaseStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalCausalitySnapshotStore>().Clear();
        scope.ServiceProvider.GetRequiredService<IOperationalSituationSnapshotStore>().Clear();
    }
}
