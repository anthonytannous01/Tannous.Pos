namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Best-effort append-only operational audit persistence. Failures must never propagate to callers.
/// </summary>
public interface IOperationalAuditRecorder
{
    Task RecordAsync(OperationalAuditRecordRequest request, CancellationToken cancellationToken = default);
}
