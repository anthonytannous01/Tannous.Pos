using System.Net;
using System.Net.Http.Json;
using BCrypt.Net;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class RateLimitingTests : IntegrationTestBase
{
    [Fact]
    public async Task AuthRateLimiting_ShouldReturn429_WhenExceedingLimit()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act - Make more than 5 requests in a minute
        var loginRequest = new
        {
            Username = "owner",
            Password = "password"
        };

        var responses = new List<HttpResponseMessage>();
        
        // Make 6 requests (exceeding the 5 req/min limit)
        for (int i = 0; i < 6; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);
            responses.Add(response);
        }

        // Assert
        var lastResponse = responses.Last();
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        
        // Verify Retry-After header is present
        Assert.True(
            lastResponse.Headers.Contains("Retry-After"),
            $"Expected Retry-After header on 429 response, got headers: {string.Join(", ", lastResponse.Headers.Select(h => h.Key))}");
    }

    [Fact]
    public async Task DeviceRateLimiting_ShouldReturn429_WhenExceedingLimit()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("test-device-rate-limit");
        SetIdempotencyKey("test-key-001");

        // Create a simple order request
        var orderRequest = new
        {
            CustomerId = (Guid?)null,
            OrderLines = new[]
            {
                new
                {
                    MenuItemId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 10.99m,
                    AddOns = new object[0]
                }
            },
            Notes = "Rate limit test"
        };

        var responses = new List<HttpResponseMessage>();
        
        // Make 61 requests (exceeding the 60 req/min limit)
        for (int i = 0; i < 61; i++)
        {
            SetIdempotencyKey($"test-key-{i:D3}");
            var response = await _client.PostAsJsonAsync("/api/v1.0/orders", orderRequest);
            responses.Add(response);
        }

        // Assert
        var lastResponse = responses.Last();
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        
        // Verify Retry-After header is present
        Assert.True(
            lastResponse.Headers.Contains("Retry-After"),
            $"Expected Retry-After header on 429 response, got headers: {string.Join(", ", lastResponse.Headers.Select(h => h.Key))}");
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        // Create test user
        var user = new Tannous.Pos.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = "owner",
            Email = "owner@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.Owner,
            FirstName = "Test",
            LastName = "Owner",
            IsActive = true
        };
        context.Users.Add(user);

        // Create test device
        var device = new Tannous.Pos.Domain.Entities.Device
        {
            Id = Guid.NewGuid(),
            DeviceId = "test-device-rate-limit",
            Name = "Test Device",
            DeviceType = "POS",
            IsActive = true
        };
        context.Devices.Add(device);

        await context.SaveChangesAsync();
    }
}
