using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Orders;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OrderSettlementIntegrationTests : IntegrationTestBase
{
    public OrderSettlementIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Finalize_exact_payment_sets_settlement_fields()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("settle-exact-001");

        var (orderId, totalOwed) = await CreateOpenOrderAsync(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = totalOwed, TransactionId = "TXN-EXACT" }
                }
            });
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var order = await scope.ServiceProvider.GetRequiredService<PosDbContext>()
            .Orders.AsNoTracking()
            .FirstAsync(o => o.Id == orderId);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(totalOwed, order.AmountTendered);
        Assert.Equal(0m, order.ChangeDue);
        Assert.Equal(totalOwed, order.NetCapturedAmount);
    }

    [SkippableFact]
    public async Task Finalize_overpayment_records_change_due_and_net_captured()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("settle-over-001");

        var (orderId, totalOwed) = await CreateOpenOrderAsync(token);
        const decimal tendered = 50.00m;

        var response = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = tendered, TransactionId = "TXN-OVER" }
                }
            });
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var order = await scope.ServiceProvider.GetRequiredService<PosDbContext>()
            .Orders.AsNoTracking()
            .FirstAsync(o => o.Id == orderId);

        Assert.Equal(tendered, order.AmountTendered);
        Assert.Equal(tendered - totalOwed, order.ChangeDue);
        Assert.Equal(totalOwed, order.NetCapturedAmount);
    }

    [SkippableFact]
    public async Task Finalize_underpayment_is_rejected()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("settle-under-001");

        var (orderId, totalOwed) = await CreateOpenOrderAsync(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = totalOwed - 1.00m, TransactionId = "TXN-UNDER" }
                }
            });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var order = await scope.ServiceProvider.GetRequiredService<PosDbContext>()
            .Orders.AsNoTracking()
            .FirstAsync(o => o.Id == orderId);
        Assert.NotEqual(OrderStatus.Paid, order.Status);
    }

    [SkippableFact]
    public async Task Paid_void_refund_equals_net_captured_not_tendered_when_change_due()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("settle-void-fin-001");

        var (orderId, totalOwed) = await CreateOpenOrderAsync(token);
        const decimal tendered = 50.00m;

        var finalize = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = tendered, TransactionId = "TXN-OVER-VOID" }
                }
            });
        finalize.EnsureSuccessStatusCode();

        SetIdempotencyKey("settle-void-001");
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/void",
            new { reason = "settlement refund test" });
        voidResponse.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        var refund = await db.PaymentRefunds.AsNoTracking().SingleAsync(r => r.OrderId == orderId);

        Assert.Equal(order.NetCapturedAmount, refund.Amount);
        Assert.Equal(totalOwed, refund.Amount);
        Assert.True(refund.Amount < tendered);
        Assert.True(order.ChangeDue > 0);
    }

    [SkippableFact]
    public async Task Paid_void_idempotent_retry_does_not_duplicate_refund_after_overpayment()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var (orderId, totalOwed) = await CreateOpenOrderAsync(token);
        SetIdempotencyKey("settle-void-idem-fin");
        await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = totalOwed + 5.00m, TransactionId = "TXN-IDEM" }
                }
            });

        SetIdempotencyKey("settle-void-idem");
        var body = new { reason = "idem" };
        var v1 = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", body);
        var v2 = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", body);
        v1.EnsureSuccessStatusCode();
        v2.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
        var refund = await db.PaymentRefunds.SingleAsync(r => r.OrderId == orderId);
        Assert.Equal(totalOwed, refund.Amount);
    }

    private async Task<(Guid OrderId, decimal TotalOwed)> CreateOpenOrderAsync(string token)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var menuItem = await ctx.MenuItems.FirstAsync();

        var create = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItem.Id, 10.00m, notes: "settlement test"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var subTotal = 10.00m;
        var tax = OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subTotal);
        var totalOwed = subTotal + tax;
        return (orderId, totalOwed);
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Settlement Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);
        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Settlement Item",
            Price = 10.00m,
            CategoryId = category.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
