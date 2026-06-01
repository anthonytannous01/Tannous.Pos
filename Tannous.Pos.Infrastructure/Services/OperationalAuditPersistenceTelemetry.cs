using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>In-process audit persistence telemetry (not replicated across instances).</summary>
public sealed class OperationalAuditPersistenceTelemetry : IOperationalAuditPersistenceTelemetry
{
    private int _recentFailureCount;
    private string? _lastFailureClassification;

    public void RecordSuccess()
    {
        // Success does not reset failure history — operators need visibility of recent strain.
    }

    public void RecordFailure(string failureClassification)
    {
        Interlocked.Increment(ref _recentFailureCount);
        _lastFailureClassification = failureClassification;
    }

    public int GetRecentFailureCount() => Volatile.Read(ref _recentFailureCount);

    public string? GetLastFailureClassification() => _lastFailureClassification;
}
