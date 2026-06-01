namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Best-effort in-process diagnostics cache invalidation (never fails business paths).
/// GOVERNANCE / NON-GOAL: no distributed invalidation; no DB triggers; no background workers.
/// </summary>
public interface IOperationalDiagnosticsCacheInvalidator
{
    void InvalidateAfterReconciliationWorkflow();

    void InvalidateAfterConflictRecorded(string conflictType, string? deviceId, string? operationId);

    void InvalidateAllDiagnosticsCaches();
}
