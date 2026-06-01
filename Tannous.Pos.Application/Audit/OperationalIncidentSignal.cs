namespace Tannous.Pos.Application.Audit;

/// <summary>Internal correlation signal (not exposed on mobile wire).</summary>
public sealed class OperationalIncidentSignal
{
    public string IncidentType { get; init; } = string.Empty;
    public string Subsystem { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public Guid? EntityId { get; init; }
    public string? ConflictType { get; init; }
    public string? AuditAction { get; init; }
    public string? ResolutionStatus { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string CorrelationKey { get; init; } = string.Empty;
}
