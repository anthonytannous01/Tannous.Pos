using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net.Http.Json;
using System.Text.Json;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Orders;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;
using Tannous.Pos.Integration.Infrastructure;
using Xunit;

namespace Tannous.Pos.Integration;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncDisposable
{
    private static readonly ConcurrentDictionary<Type, ClassTestDatabase> SharedClassDatabases = new();

    private readonly IntegrationPostgresFixture _postgresFixture;
    private ClassTestDatabase? _sharedDatabase;

    protected WebApplicationFactory<Program> _factory = null!;
    protected HttpClient _client = null!;
    private string? _cachedOwnerAuthToken;

    protected IntegrationTestBase(IntegrationPostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    protected async Task InitializeDatabaseAsync()
    {
        await _postgresFixture.EnsureInitializedAsync();

        if (_postgresFixture.SkipReason != null)
            Skip.If(true, _postgresFixture.SkipReason);

        _sharedDatabase = SharedClassDatabases.GetOrAdd(GetType(), _ => new ClassTestDatabase());
        await _sharedDatabase.Lock.WaitAsync();

        try
        {
            if (_sharedDatabase.ConnectionString is null)
            {
                _sharedDatabase.ConnectionString = await _postgresFixture.AllocateDatabaseAsync();
                await _postgresFixture.EnsureDatabaseSchemaAsync(_sharedDatabase.ConnectionString);
                _sharedDatabase.Factory = _postgresFixture.CreateWebApplicationFactory(_sharedDatabase.ConnectionString);

                using var seedScope = _sharedDatabase.Factory.Services.CreateScope();
                var seedContext = seedScope.ServiceProvider.GetRequiredService<PosDbContext>();
                await SeedStandardIntegrationIdentityAsync(seedContext);
                await SeedTestDataAsync(seedContext);
                await EnsureNormalizedUsernamesAsync(seedContext);
                _sharedDatabase.Seeded = true;
            }

            _factory = _sharedDatabase.Factory!;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://localhost")
            });
            _sharedDatabase.AddRef();
        }
        finally
        {
            _sharedDatabase.Lock.Release();
        }
    }

    private sealed class ClassTestDatabase
    {
        private int _activeTests;

        public SemaphoreSlim Lock { get; } = new(1, 1);
        public string? ConnectionString { get; set; }
        public WebApplicationFactory<Program>? Factory { get; set; }
        public bool Seeded { get; set; }

        public void AddRef() => Interlocked.Increment(ref _activeTests);

        public bool ReleaseRef() => Interlocked.Decrement(ref _activeTests) == 0;
    }

    /// <summary>Owner user and default device required for auth and device-validated mutations.</summary>
    protected static async Task SeedStandardIntegrationIdentityAsync(PosDbContext context)
    {
        if (!await context.Users.AnyAsync(u => u.Username == "owner"))
            context.Users.Add(CreateOwnerUser());

        if (!await context.Devices.AnyAsync(d => d.DeviceId == "test-device-001"))
        {
            context.Devices.Add(new Device
            {
                Id = Guid.NewGuid(),
                DeviceId = "test-device-001",
                Name = "Integration Test Device",
                DeviceType = "Terminal",
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureNormalizedUsernamesAsync(PosDbContext context)
    {
        var users = await context.Users
            .Where(u => string.IsNullOrWhiteSpace(u.NormalizedUsername))
            .ToListAsync();

        if (users.Count == 0)
            return;

        foreach (var user in users)
            user.NormalizedUsername = user.Username.ToUpperInvariant();

        await context.SaveChangesAsync();
    }

    protected async Task<Guid> GetDefaultMenuItemIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        return await context.MenuItems.Select(m => m.Id).FirstAsync();
    }

    protected async Task<InventoryItem?> GetInventoryItemByIngredientAsync(Guid ingredientId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        return await context.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(ii => ii.IngredientId == ingredientId);
    }

    protected async Task<IReadOnlyList<InventoryMovement>> GetInventoryMovementsForOrderAsync(Guid orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var orderNumber = await context.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.OrderNumber)
            .FirstAsync();
        return await context.InventoryMovements.AsNoTracking()
            .Where(im => im.Reference != null && im.Reference.Contains(orderNumber))
            .ToListAsync();
    }

    protected static async Task<Guid> ReadCreatedOrderIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("id", out var idProperty) || json.TryGetProperty("Id", out idProperty))
            return idProperty.GetGuid();

        throw new InvalidOperationException("Create order response did not contain an order id.");
    }

    protected async Task TransitionOrderToOpenAsync(Guid orderId)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/status",
            new { Status = OrderStatus.Open });
        await AssertHttpSuccessAsync(response, "transition order to open");
    }

    protected static decimal TotalWithLegacyTax(decimal subTotal) =>
        subTotal + OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subTotal);

    protected static async Task AssertHttpSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"{operation} failed with HTTP {(int)response.StatusCode}: {body}");
    }

    protected static async Task AssertSyncOperationSuccessAsync(HttpResponseMessage response, int operationIndex = 0)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("results")[operationIndex];
        var success = result.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        var error = result.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null;
        Assert.True(success, $"Expected sync operation success but got: {error ?? "unknown error"}");
    }

    protected static object BuildCreateOrderPayload(
        Guid menuItemId,
        decimal unitPrice,
        int quantity = 1,
        string? notes = null) =>
        new
        {
            OrderType = OrderType.DineIn,
            CustomerId = (Guid?)null,
            OrderLines = new[]
            {
                new
                {
                    MenuItemId = menuItemId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    AddOns = Array.Empty<object>()
                }
            },
            Notes = notes ?? "integration test order"
        };

    protected static User CreateOwnerUser(string username = "owner", string password = "password") =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            Email = $"{username}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = Tannous.Pos.Domain.Enums.Role.Owner,
            FirstName = "Test",
            LastName = "Owner",
            IsActive = true
        };

    protected virtual async Task SeedTestDataAsync(PosDbContext context)
    {
        await Task.CompletedTask;
    }

    protected async Task<string> GetAuthTokenAsync(string username = "owner", string password = "password")
    {
        if (!_client.DefaultRequestHeaders.Contains("Device-Id"))
            SetDeviceId();

        var loginRequest = new
        {
            Username = username,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (result.ValueKind == JsonValueKind.Object &&
            result.TryGetProperty("accessToken", out var accessToken))
        {
            var token = accessToken.GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(token), "Login succeeded but accessToken was empty.");
            if (username == "owner" && password == "password")
                _cachedOwnerAuthToken = token;
            return token;
        }

        throw new InvalidOperationException("Login response did not contain accessToken.");
    }

    protected void SetAuthHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetDeviceId(string deviceId = "test-device-001")
    {
        _client.DefaultRequestHeaders.Remove("Device-Id");
        _client.DefaultRequestHeaders.Add("Device-Id", deviceId);
    }

    protected void SetIdempotencyKey(string key = "")
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        if (!string.IsNullOrEmpty(key))
        {
            _client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        }
    }

    /// <summary>Clears all operational diagnostics cache categories (use after seeding fresh data).</summary>
    protected void ResetOperationalDiagnosticsCaches()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>().RemoveAllDiagnosticsCaches();
    }

    /// <summary>Clears upstream cache categories only (resilience, reconciliation, incidents).</summary>
    protected void ClearOperationalDiagnosticsUpstreamCaches()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>();
        cache.Remove(
            OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
            OperationalDiagnosticsCacheCategories.ResilienceMetrics);
        cache.Remove(
            OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
            OperationalDiagnosticsCacheCategories.ReconciliationSummary);
        cache.Remove(
            OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey,
            OperationalDiagnosticsCacheCategories.IncidentGroups);
    }

    /// <summary>Clears alert-layer caches while preserving upstream cache entries.</summary>
    protected void ClearOperationalAlertLayerCachesOnly()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>();
        cache.Remove(
            OperationalDiagnosticsCacheConstants.AlertSignalsCacheKey,
            OperationalDiagnosticsCacheCategories.AlertSignals);
        cache.Remove(
            OperationalDiagnosticsCacheConstants.AlertSummaryCacheKey,
            OperationalDiagnosticsCacheCategories.AlertSummary);
    }

    protected IOperationalDiagnosticsCacheTelemetry GetOperationalDiagnosticsCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }

    protected static long GetOperationalCacheCategoryHits(
        IOperationalDiagnosticsCacheTelemetry telemetry,
        string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Hits : 0;

    /// <summary>Resets governance pressure flags, lifecycle epochs, and optional diagnostics caches (integration isolation).</summary>
    protected void ResetOperationalGovernancePressureState(bool clearDiagnosticsCaches = true)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<IOperationalDiagnosticsPressureResetCoordinator>()
            .ResetGovernanceState(clearDiagnosticsCaches);
    }

    /// <summary>Full governance stabilization reset (pressure + caches + convergence windows).</summary>
    protected void ResetOperationalGovernanceStabilization()
    {
        ResetOperationalGovernancePressureState(clearDiagnosticsCaches: true);
    }

    /// <summary>Resets governance snapshots, fingerprints, caches, and telemetry baselines for deterministic integration tests.</summary>
    protected void ResetOperationalGovernanceDiagnosticsState()
    {
        ResetOperationalGovernanceStabilization();
        StabilizeOperationalGovernanceTelemetry();
    }

    /// <summary>Clears governance-only telemetry counters that can carry over between integration assertions.</summary>
    protected void StabilizeOperationalGovernanceTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<IOperationalDiagnosticsCacheTelemetry>()
            .ResetGovernanceStabilizationBaseline();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        NpgsqlConnection.ClearAllPools();

        if (_sharedDatabase is null || !_sharedDatabase.ReleaseRef())
            return;

        var connectionString = _sharedDatabase.ConnectionString;
        try
        {
            if (_sharedDatabase.Factory is IAsyncDisposable asyncFactory)
                await asyncFactory.DisposeAsync();
            else
                _sharedDatabase.Factory?.Dispose();

            if (!string.IsNullOrWhiteSpace(connectionString))
                await _postgresFixture.DropDatabaseAsync(connectionString);

            SharedClassDatabases.TryRemove(GetType(), out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Integration environment observability: failed to drop shared class test database (non-fatal). {ex.Message}");
        }
    }
}
