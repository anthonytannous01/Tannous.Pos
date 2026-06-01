namespace Tannous.Pos.Application.Audit;

/// <summary>In-process audit persistence failure visibility (best-effort; not a durable queue).</summary>
public interface IOperationalAuditPersistenceTelemetry
{
    void RecordSuccess();
    void RecordFailure(string failureClassification);
    int GetRecentFailureCount();
    string? GetLastFailureClassification();
}
