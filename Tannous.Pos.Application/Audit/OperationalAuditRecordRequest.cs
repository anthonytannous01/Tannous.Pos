namespace Tannous.Pos.Application.Audit;

public sealed class OperationalAuditRecordRequest
{
    public string Category { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public Guid? OrderId { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public string? CorrelationId { get; init; }
    public string Severity { get; init; } = OperationalAuditSeverity.Information;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary>When true, skips insert if same device+operation+action already exists (replay-safe).</summary>
    public bool DedupeByDeviceOperationAndAction { get; init; }
}
