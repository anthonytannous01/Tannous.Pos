namespace Tannous.Pos.Application.Audit;

public sealed class OperationalAuditTimelineEntryDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public Guid? OrderId { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public string? CorrelationId { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? MetadataJson { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
