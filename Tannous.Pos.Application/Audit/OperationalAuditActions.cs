namespace Tannous.Pos.Application.Audit;

public static class OperationalAuditActions
{
    public const string FinalizeSuccess = "FinalizeSuccess";
    public const string FinalizeReplayShortCircuit = "FinalizeReplayShortCircuit";
    public const string VoidSuccess = "VoidSuccess";
    public const string RefundPersisted = "RefundPersisted";
    public const string SettlementOverpayment = "SettlementOverpayment";
    public const string SettlementUnderpaymentRejected = "SettlementUnderpaymentRejected";
    public const string InventoryDeductionPass = "InventoryDeductionPass";
    public const string NegativeStockDetected = "NegativeStockDetected";
    public const string ReversalMovementPersisted = "ReversalMovementPersisted";
    public const string ReplayMismatch = "ReplayMismatch";
    public const string StaleOfflineMutation = "StaleOfflineMutation";
    public const string LifecycleStateConflict = "LifecycleStateConflict";
    public const string PartialBatchReconciliation = "PartialBatchReconciliation";
    public const string ConcurrencyConflict = "ConcurrencyConflict";
    public const string DurableReplayShortCircuit = "DurableReplayShortCircuit";
    public const string PlaceholderOperationExecuted = "PlaceholderOperationExecuted";
    public const string MixedBatchOutcomes = "MixedBatchOutcomes";
}
