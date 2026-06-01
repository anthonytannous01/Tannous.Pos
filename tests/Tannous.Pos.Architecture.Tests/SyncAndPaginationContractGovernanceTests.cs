using System.Text.Json;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Sync;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Guards stable JSON contracts for sync and list APIs (camelCase, required wire names).
/// </summary>
public class SyncAndPaginationContractGovernanceTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PushRequestDto_uses_operationId_wire_name_for_outbox_operations()
    {
        var dto = new PushRequestDto
        {
            DeviceId = "d1",
            Operations = new List<OutboxOperationDto>
            {
                new()
                {
                    Type = "FinalizeOrder",
                    OpId = "op-1",
                    Payload = new Dictionary<string, object?>()
                }
            }
        };

        var json = JsonSerializer.Serialize(dto, Json);
        Assert.Contains("\"operationId\":\"op-1\"", json, StringComparison.Ordinal);

        var back = JsonSerializer.Deserialize<PushRequestDto>(json, Json);
        Assert.NotNull(back?.Operations.Single().OpId);
        Assert.Equal("op-1", back!.Operations.Single().OpId);
    }

    [Fact]
    public void PaginatedResponseDto_deserializes_standard_pagination_shape()
    {
        const string json = """{"items":[],"total":0,"page":1,"pageSize":20}""";
        var dto = JsonSerializer.Deserialize<PaginatedResponseDto<string>>(json, Json);
        Assert.NotNull(dto);
        Assert.Empty(dto!.Items);
        Assert.Equal(0, dto.Total);
        Assert.Equal(1, dto.Page);
        Assert.Equal(20, dto.PageSize);
    }
}
