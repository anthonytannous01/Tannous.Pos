using System.Text.Json;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Application.DTOs.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// JSON wire-shape checks for settings, pagination, sync, and push (camelCase; no DTO field renames).
/// </summary>
public class WireContractGovernanceTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void SettingsDto_serializes_storeName_wire_name()
    {
        var dto = new SettingsDto
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store",
            Currency = "USD",
            TaxRate = 0.1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(dto, Json);
        Assert.Contains("\"storeName\":", json, StringComparison.Ordinal);
        // Android may still map businessName client-side; server must keep storeName stable.
    }

    [Fact]
    public void PaginatedResponseDto_serializes_full_pagination_contract()
    {
        var dto = new PaginatedResponseDto<string>
        {
            Items = new[] { "a" },
            Total = 42,
            Page = 2,
            PageSize = 10
        };

        var json = JsonSerializer.Serialize(dto, Json);
        Assert.Contains("\"items\":", json, StringComparison.Ordinal);
        Assert.Contains("\"page\":", json, StringComparison.Ordinal);
        Assert.Contains("\"pageSize\":", json, StringComparison.Ordinal);
        Assert.Contains("\"total\":", json, StringComparison.Ordinal);
        Assert.Contains("\"totalPages\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void PullResponseDto_serializes_cursor_upserts_deletes()
    {
        var dto = new PullResponseDto
        {
            Cursor = "x",
            Upserts = new UpsertsDto(),
            Deletes = new DeletesDto()
        };

        var json = JsonSerializer.Serialize(dto, Json);
        Assert.Contains("\"cursor\":", json, StringComparison.Ordinal);
        Assert.Contains("\"upserts\":", json, StringComparison.Ordinal);
        Assert.Contains("\"deletes\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void PushRequestDto_serializes_operationId_type_payload_per_operation()
    {
        var dto = new PushRequestDto
        {
            DeviceId = "dev",
            Operations =
            [
                new OutboxOperationDto
                {
                    Type = "CreateOrder",
                    OpId = "op-99",
                    Payload = new Dictionary<string, object?> { ["k"] = 1 }
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, Json);
        Assert.Contains("\"deviceId\":", json, StringComparison.Ordinal);
        Assert.Contains("\"operationId\":\"op-99\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"CreateOrder\"", json, StringComparison.Ordinal);
        Assert.Contains("\"payload\":", json, StringComparison.Ordinal);
    }
}
