namespace Tannous.Pos.Application.Audit;

/// <summary>Operational audit actions surfaced by the internal conflict diagnostics endpoint.</summary>
public static class OperationalAuditConflictActions
{
    public static readonly string[] All =
    {
        OperationalAuditActions.ReplayMismatch,
        OperationalAuditActions.ConcurrencyConflict,
        OperationalAuditActions.NegativeStockDetected,
        OperationalAuditActions.LifecycleStateConflict,
        OperationalAuditActions.PartialBatchReconciliation,
        OperationalAuditActions.StaleOfflineMutation,
        OperationalAuditActions.MixedBatchOutcomes
    };
}
