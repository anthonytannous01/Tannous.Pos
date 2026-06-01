namespace Tannous.Pos.Application.Audit;

/// <summary>Safe projection of an operational audit record for internal diagnostics (no payloads/stacks).</summary>
public sealed class OperationalAuditTimelineItemDto
{
    public Guid Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public Guid? OrderId { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
