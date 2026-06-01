namespace Tannous.Pos.Application.Audit;

/// <summary>Internal chronological reconstruction of operational audit records (not exposed on API wire).</summary>
public interface IOperationalAuditTimelineService
{
    Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
