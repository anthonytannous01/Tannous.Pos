namespace Tannous.Pos.Application.Audit;

public interface IOperationalIncidentCorrelationService
{
    Task<OperationalIncidentSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalIncidentPageDto> GetHighRiskAsync(CancellationToken cancellationToken = default);

    Task<OperationalIncidentPageDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<OperationalIncidentPageDto> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<OperationalIncidentPageDto> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default);

    Task<OperationalCascadingDegradationDto> GetCascadingDegradationAsync(CancellationToken cancellationToken = default);

    ForensicIncidentCorrelationDto BuildForensicCorrelation(
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        ForensicTruncationFlags truncationFlags);
}
