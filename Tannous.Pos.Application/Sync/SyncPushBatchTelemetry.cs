using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Aggregated per-push batch counters for operational observability (internal only).
/// </summary>
public sealed class SyncPushBatchTelemetry
{
    public int BatchSize { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public int ConflictCount { get; private set; }
    public int ReplayShortCircuitCount { get; private set; }
    public int PlaceholderCount { get; private set; }
    public int ValidationFailureCount { get; private set; }
    public int RetryableFailureCount { get; private set; }
    public int NonRetryableFailureCount { get; private set; }
    public int CustomerShiftReplayShortCircuitCount { get; private set; }
    public int InventoryReplayShortCircuitCount { get; private set; }
    public int InventoryOperationFailureCount { get; private set; }

    public void Record(SyncOperationOutcomeClassification classification, OpResultDto result, string? operationType = null)
    {
        BatchSize++;

        switch (classification)
        {
            case SyncOperationOutcomeClassification.Success:
            case SyncOperationOutcomeClassification.ReplayShortCircuited:
                SuccessCount++;
                break;
            case SyncOperationOutcomeClassification.Conflict:
                ConflictCount++;
                break;
            default:
                FailureCount++;
                break;
        }

        switch (classification)
        {
            case SyncOperationOutcomeClassification.ReplayShortCircuited:
                ReplayShortCircuitCount++;
                if (!string.IsNullOrWhiteSpace(operationType))
                {
                    if (DurableSyncReplayProtectedTypes.IsCustomerOrShiftProtected(operationType))
                        CustomerShiftReplayShortCircuitCount++;
                    if (DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType))
                        InventoryReplayShortCircuitCount++;
                }
                break;
            case SyncOperationOutcomeClassification.PlaceholderOperation:
                PlaceholderCount++;
                break;
            case SyncOperationOutcomeClassification.ValidationFailure:
                ValidationFailureCount++;
                break;
            case SyncOperationOutcomeClassification.RetryableFailure:
                RetryableFailureCount++;
                break;
            case SyncOperationOutcomeClassification.NonRetryableFailure:
                NonRetryableFailureCount++;
                break;
        }

        if (!string.IsNullOrWhiteSpace(operationType)
            && DurableSyncReplayProtectedTypes.IsInventoryProtected(operationType)
            && classification is not SyncOperationOutcomeClassification.Success
                and not SyncOperationOutcomeClassification.ReplayShortCircuited)
        {
            InventoryOperationFailureCount++;
        }
    }

    public bool IsPartialBatchRisk =>
        SyncOperationOutcomeClassifier.IsMixedBatch(SuccessCount, FailureCount, ConflictCount);

    public bool HasReplayMixedWithFailureOrConflict =>
        ReplayShortCircuitCount > 0 && (FailureCount > 0 || ConflictCount > 0);

    public bool HasMixedPlaceholderAndReplayVisibility =>
        CustomerShiftReplayShortCircuitCount > 0 &&
        (FailureCount > 0 || ConflictCount > 0 || PlaceholderCount > 0);

    public bool HasMixedInventoryAndReplayVisibility =>
        InventoryReplayShortCircuitCount > 0 && (FailureCount > 0 || ConflictCount > 0);

    public bool HasPartialBatchInventoryReconciliation =>
        InventoryOperationFailureCount > 0 && IsPartialBatchRisk;
}
