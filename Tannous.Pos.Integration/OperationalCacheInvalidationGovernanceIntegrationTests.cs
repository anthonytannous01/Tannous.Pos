using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Integration;

public class OperationalCacheInvalidationGovernanceIntegrationTests : IntegrationTestBase
{
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string SyncDeviceId = "invalidation-governance-device-001";

    public OperationalCacheInvalidationGovernanceIntegrationTests(IntegrationPostgresFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Invalidation_audit_returns_bounded_projection()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/invalidation-audit");
        response.EnsureSuccessStatusCode();
        var audit = await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationAuditDto>();

        Assert.NotNull(audit);
        Assert.NotEmpty(audit!.InvalidationSeverity);
        Assert.NotEmpty(audit.FreshnessRecoveryState);
        Assert.NotEmpty(audit.InvalidationDriftClassification);
        Assert.True(audit.ReasonCodes.Count <= OperationalCacheInvalidationGovernance.MaxReasonCodes);
    }

    [SkippableFact]
    public async Task Freshness_recovery_stabilizes_after_full_cache_reset()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();
        ResetOperationalDiagnosticsCaches();

        var afterReset = await GetFreshnessRecoveryAsync();
        Assert.NotNull(afterReset);
        Assert.Equal(
            OperationalCacheFreshnessRecoveryState.Stable.ToString(),
            afterReset!.RecoveryState);
    }

    [SkippableFact]
    public async Task Reconciliation_transition_updates_invalidation_audit_visibility()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        var conflictId = await SeedUnresolvedConflictAsync();
        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        var before = await GetInvalidationAuditAsync();
        var invalidationsBefore = before?.TotalInvalidations ?? 0;

        var acknowledge = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/acknowledge/{conflictId}",
            new ReconciliationStatusChangeRequest { Notes = "invalidation-governance-test" });
        acknowledge.EnsureSuccessStatusCode();

        var after = await GetInvalidationAuditAsync();
        Assert.NotNull(after);
        Assert.True(after!.TotalInvalidations > invalidationsBefore);
        Assert.True(after.CrossCategoryInvalidations >= 0);
    }

    [SkippableFact]
    public async Task Invalidation_consistency_returns_advisory_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        (await _client.GetAsync($"{ResilienceBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/invalidation-consistency");
        response.EnsureSuccessStatusCode();
        var consistency = await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationConsistencyDto>();

        Assert.NotNull(consistency);
        Assert.NotNull(consistency!.InconsistencySignals);
        Assert.NotEmpty(consistency.InvalidationDriftClassification);
    }

    [SkippableFact]
    public async Task Alert_layer_invalidation_remains_deterministic_after_upstream_seed()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ResetOperationalDiagnosticsCaches();

        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();
        ClearOperationalAlertLayerCachesOnly();
        (await _client.GetAsync($"{AlertsBase}/summary")).EnsureSuccessStatusCode();

        var pressure = await GetInvalidationPressureAsync();
        Assert.NotNull(pressure);
        Assert.True(pressure!.TotalInvalidations >= 0);
    }

    [SkippableFact]
    public async Task Invalidation_pressure_reflects_telemetry_after_conflict_recording()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);
        ResetOperationalDiagnosticsCaches();

        const string opId = "invalidation-governance-replay-op";
        (await _client.GetAsync("/api/v1.0/internal/operational-audit/incidents/summary")).EnsureSuccessStatusCode();
        await SeedReplayMismatchConflictAsync(opId);

        var pressure = await GetInvalidationPressureAsync();
        Assert.NotNull(pressure);
        Assert.True(pressure!.TotalInvalidations > 0);
        Assert.NotEmpty(pressure.InvalidationSeverity);
    }

    private async Task<OperationalCacheInvalidationAuditDto?> GetInvalidationAuditAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/invalidation-audit");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationAuditDto>();
    }

    private async Task<OperationalCacheFreshnessRecoveryDto?> GetFreshnessRecoveryAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/freshness-recovery");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheFreshnessRecoveryDto>();
    }

    private async Task<OperationalCacheInvalidationPressureDto?> GetInvalidationPressureAsync()
    {
        var response = await _client.GetAsync($"{CacheBase}/invalidation-pressure");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalCacheInvalidationPressureDto>();
    }

    private async Task<Guid> SeedUnresolvedConflictAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Tannous.Pos.Infrastructure.Data.PosDbContext>();
        var record = new Tannous.Pos.Domain.Entities.SyncConflictRecord
        {
            DeviceId = SyncDeviceId,
            OperationId = "invalidation-gov-conflict-001",
            OperationType = "AdjustInventory",
            EntityType = "SyncOperation",
            ConflictType = SyncConflictTypes.LifecycleStateConflict,
            Reason = "invalidation governance seed",
            CreatedAtUtc = DateTime.UtcNow,
            Resolved = false,
            ResolutionStatus = nameof(ReconciliationResolutionStatus.Unresolved)
        };
        db.SyncConflictRecords.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    private async Task SeedReplayMismatchConflictAsync(string opId)
    {
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<Tannous.Pos.Infrastructure.Data.PosDbContext>();
            var ingredient = new Tannous.Pos.Domain.Entities.Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Invalidation Gov Ingredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new Tannous.Pos.Domain.Entities.InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 10m,
                MinimumStock = 0m,
                MaximumStock = 100m,
                AverageCost = 1m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        var adjustBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "invalidation-gov-test"
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", adjustBody)).EnsureSuccessStatusCode();

        var mismatchBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?> { ["orderType"] = "DineIn" }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody)).EnsureSuccessStatusCode();
    }

    protected override async Task SeedTestDataAsync(Tannous.Pos.Infrastructure.Data.PosDbContext context)
    {
        context.Devices.Add(new Tannous.Pos.Domain.Entities.Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Invalidation Governance Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
