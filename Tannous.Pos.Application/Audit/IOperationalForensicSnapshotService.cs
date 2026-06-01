namespace Tannous.Pos.Application.Audit;

/// <summary>Read-only aggregation of operational audit, conflict, and replay data for internal forensic export.</summary>
public interface IOperationalForensicSnapshotService
{
    Task<OperationalForensicSnapshotDto?> ExportByConflictIdAsync(
        Guid conflictId,
        CancellationToken cancellationToken = default);

    Task<OperationalForensicSnapshotDto> ExportByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OperationalForensicSnapshotDto> ExportByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<OperationalForensicSnapshotDto> ExportByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default);
}
