using System.Text.Json.Serialization;
using Tannous.Pos.Application.Serialization;

namespace Tannous.Pos.Application.DTOs.Sync;

public class PushRequestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string? SinceCursor { get; set; }
    public List<OutboxOperationDto> Operations { get; set; } = new();
}

public class OutboxOperationDto
{
    public string Type { get; set; } = string.Empty; // CreateOrder, FinalizeOrder, OpenShift, CashDrop, CreateCustomer, etc.

    /// <summary>Wire name <c>operationId</c> (mobile); stable id for the outbox operation.</summary>
    [JsonPropertyName("operationId")]
    public string OpId { get; set; } = string.Empty;

    [JsonConverter(typeof(OutboxPayloadDictionaryConverter))]
    public Dictionary<string, object?> Payload { get; set; } = new();
}

public class PushResponseDto
{
    public List<OpResultDto> Results { get; set; } = new();
    public string NewCursor { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}

public class OpResultDto
{
    /// <summary>Wire name <c>operationId</c> (mobile).</summary>
    [JsonPropertyName("operationId")]
    public string OpId { get; set; } = string.Empty;

    public bool Success { get; set; }
    public string? ServerId { get; set; }

    /// <summary>Wire name <c>error</c> (mobile); human-readable failure reason.</summary>
    [JsonPropertyName("error")]
    public string? Message { get; set; }

    public bool Conflict { get; set; }

    /// <summary>Conflict snapshot; mobile expects a string in current Kotlin model.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ServerEntity { get; set; }
}
