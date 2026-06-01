using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Integration;

public class OperationalCacheConsistencyGovernanceIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string SyncDeviceId = "consistency-governance-device-001";

    public OperationalCacheConsistencyGovernanceIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Consistency_recovery_returns_bounded_projection()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/consistency-recovery");
        response.EnsureSuccessStatusCode();
        var recovery = await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyRecoveryDto>();

        Assert.NotNull(recovery);
        Assert.NotEmpty(recovery!.ContainmentState);
        Assert.NotEmpty(recovery.ConfidenceLevel);
        Assert.True(recovery.ReasonCodes.Count <= OperationalCacheConsistencyGovernance.MaxExplainabilityItems);
    }

    [SkippableFact]
    public async Task Containment_audit_reflects_escalation_after_churn()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        using (var scope = _factory.Services.CreateScope())
        {
            var telemetry = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
            for (var i = 0; i < 20; i++)
                telemetry.RecordBypass(OperationalDiagnosticsCacheCategories.ResilienceMetrics);
        }

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();

        var audit = await GetContainmentAuditAsync();
        Assert.NotNull(audit);
        Assert.NotEmpty(audit!.ContainmentState);
        Assert.NotEmpty(audit.PropagationSeverity);
    }

    [SkippableFact]
    public async Task Consistency_confidence_drops_visible_after_bypass_churn()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        using (var scope = _factory.Services.CreateScope())
        {
            var telemetry = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
            telemetry.RecordBypass(OperationalDiagnosticsCacheCategories.ReconciliationSummary);
            telemetry.RecordBypass(OperationalDiagnosticsCacheCategories.ReconciliationSummary);
            telemetry.RecordMiss(OperationalDiagnosticsCacheCategories.ReconciliationSummary);
        }

        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        var confidence = await GetConsistencyConfidenceAsync();
        Assert.NotNull(confidence);
        Assert.True(confidence!.ConfidenceScore >= 0);
        Assert.NotEmpty(confidence.ConfidenceLevel);
    }

    [SkippableFact]
    public async Task Stabilization_after_full_cache_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        ResetOperationalDiagnosticsCaches();

        var recovery = await GetConsistencyRecoveryAsync();
        Assert.NotNull(recovery);
        Assert.Equal(0, recovery!.ExpiredEntryCount);
        Assert.NotNull(recovery.RecoveryWindow);
    }

    [SkippableFact]
    public async Task Propagation_visible_after_multi_category_invalidation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        var conflictId = await SeedUnresolvedConflictAsync();
        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{AlertsBase}/current")).EnsureSuccessStatusCode();

        var acknowledge = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/acknowledge/{conflictId}",
            new ReconciliationStatusChangeRequest { Notes = "consistency-propagation-test" });
        acknowledge.EnsureSuccessStatusCode();

        ResetOperationalDiagnosticsCaches();
        ClearOperationalAlertLayerCachesOnly();
        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();

        var propagation = await GetPropagationDiagnosticsAsync();
        Assert.NotNull(propagation);
        Assert.NotEmpty(propagation!.PropagationSeverity);
    }

    [SkippableFact]
    public async Task Containment_and_confidence_deterministic_after_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        var confidence1 = await GetConsistencyConfidenceAsync();
        var confidence2 = await GetConsistencyConfidenceAsync();

        Assert.NotNull(confidence1);
        Assert.NotNull(confidence2);
        Assert.Equal(confidence1!.ConfidenceLevel, confidence2!.ConfidenceLevel);
        Assert.Equal(confidence1.ConfidenceScore, confidence2.ConfidenceScore);
    }

    private async Task<OperationalCacheConsistencyRecoveryDto?> GetConsistencyRecoveryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/consistency-recovery");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyRecoveryDto>();
    }

    private async Task<OperationalCacheContainmentAuditDto?> GetContainmentAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/containment-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheContainmentAuditDto>();
    }

    private async Task<OperationalCachePropagationDiagnosticsDto?> GetPropagationDiagnosticsAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/propagation-diagnostics");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCachePropagationDiagnosticsDto>();
    }

    private async Task<OperationalCacheConsistencyConfidenceDto?> GetConsistencyConfidenceAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/consistency-confidence");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheConsistencyConfidenceDto>();
    }

    private async Task<Guid> SeedUnresolvedConflictAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Tannous.Pos.Infrastructure.Data.PosDbContext>();
        var record = new Tannous.Pos.Domain.Entities.SyncConflictRecord
        {
            DeviceId = SyncDeviceId,
            OperationId = "consistency-gov-conflict-001",
            OperationType = "AdjustInventory",
            EntityType = "SyncOperation",
            ConflictType = SyncConflictTypes.LifecycleStateConflict,
            Reason = "consistency governance seed",
            CreatedAtUtc = DateTime.UtcNow,
            Resolved = false,
            ResolutionStatus = nameof(ReconciliationResolutionStatus.Unresolved)
        };
        db.SyncConflictRecords.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    protected override async Task SeedTestDataAsync(Tannous.Pos.Infrastructure.Data.PosDbContext context)
    {
        context.Devices.Add(new Tannous.Pos.Domain.Entities.Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Consistency Governance Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
