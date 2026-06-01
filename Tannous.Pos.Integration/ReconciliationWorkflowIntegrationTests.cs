using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class ReconciliationWorkflowIntegrationTests : IntegrationTestBase
{
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";
    private const string SyncDeviceId = "recon-workflow-device-001";

    public ReconciliationWorkflowIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Unresolved_query_returns_open_conflicts_after_replay_mismatch()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        var conflictId = await SeedReplayMismatchConflictAsync();

        var response = await _client.GetAsync($"{ReconciliationBase}/unresolved?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<SyncConflictPageDto>();
        Assert.NotNull(page);
        Assert.Contains(page!.Items, i => i.Id == conflictId);
        Assert.Equal(nameof(ReconciliationResolutionStatus.Unresolved), page.Items.First(i => i.Id == conflictId).ResolutionStatus);
    }

    [SkippableFact]
    public async Task Acknowledge_flow_updates_status_and_creates_audit_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync();

        var response = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/acknowledge/{conflictId}",
            new { notes = "operator acknowledged" });
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<SyncConflictItemDto>();
        Assert.Equal(nameof(ReconciliationResolutionStatus.Acknowledged), item!.ResolutionStatus);

        await using var scope = _factory.Services.CreateAsyncScope();
        var audits = await scope.ServiceProvider.GetRequiredService<IOperationalAuditQueryService>()
            .GetReconciliationWorkflowAuditAsync(new OperationalAuditQueryFilter(), 1, 50);
        Assert.Contains(audits.Items, a => a.Action == OperationalAuditReconciliationActions.ConflictAcknowledged);
    }

    [SkippableFact]
    public async Task Resolve_flow_marks_conflict_resolved_with_audit()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync();

        var response = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/resolve/{conflictId}",
            new { notes = "manually resolved" });
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<SyncConflictItemDto>();
        Assert.Equal(nameof(ReconciliationResolutionStatus.Resolved), item!.ResolutionStatus);
        Assert.NotNull(item.ResolvedAtUtc);
    }

    [SkippableFact]
    public async Task Ignore_flow_marks_conflict_ignored()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync();

        var response = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/ignore/{conflictId}",
            new { notes = "benign duplicate" });
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<SyncConflictItemDto>();
        Assert.Equal(nameof(ReconciliationResolutionStatus.Ignored), item!.ResolutionStatus);
    }

    [SkippableFact]
    public async Task Summary_returns_counts_including_replay_mismatch()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync();

        var response = await _client.GetAsync($"{ReconciliationBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReconciliationSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary!.UnresolvedCount >= 1);
        Assert.True(summary.ReplayMismatchCount >= 1);
    }

    [SkippableFact]
    public async Task Long_notes_are_truncated_on_resolve()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync();
        var longNotes = new string('x', ReconciliationWorkflowConstants.MaxResolutionNotesLength + 100);

        var response = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/resolve/{conflictId}",
            new { notes = longNotes });
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<SyncConflictItemDto>();
        Assert.NotNull(item!.ResolutionNotes);
        Assert.True(item.ResolutionNotes!.Length <= ReconciliationWorkflowConstants.MaxResolutionNotesLength);
    }

    [SkippableFact]
    public async Task Cashier_is_denied_reconciliation_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{ReconciliationBase}/unresolved");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> SeedReplayMismatchConflictAsync()
    {
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Recon Workflow Ingredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 10m,
                MinimumStock = 0,
                MaximumStock = 100m,
                AverageCost = 1m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "recon-workflow-mismatch-001";
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
                        ["reason"] = "workflow-test"
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

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var record = await db.SyncConflictRecords
            .Where(r => r.DeviceId == SyncDeviceId && r.OperationId == opId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstAsync();
        return record.Id;
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "cashier",
            NormalizedUsername = "CASHIER",
            Email = "cashier@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.Cashier,
            FirstName = "Test",
            LastName = "Cashier",
            IsActive = true
        });
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Reconciliation Workflow Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
