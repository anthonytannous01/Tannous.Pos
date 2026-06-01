using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Classifies sync push operation results for operational logging (preserves existing OpResultDto messages).
/// </summary>
public static class SyncOperationOutcomeClassifier
{
    private static readonly HashSet<string> PlaceholderOperationTypes = new(StringComparer.Ordinal)
    {
        "CreateCustomer",
        "OpenShift"
    };

    public static SyncOperationOutcomeClassification Classify(
        OutboxOperationDto operation,
        OpResultDto result,
        bool replayShortCircuited,
        bool unexpectedException)
    {
        if (replayShortCircuited)
            return SyncOperationOutcomeClassification.ReplayShortCircuited;

        if (PlaceholderOperationTypes.Contains(operation.Type))
            return SyncOperationOutcomeClassification.PlaceholderOperation;

        if (result.Conflict)
            return SyncOperationOutcomeClassification.Conflict;

        if (result.Success)
            return SyncOperationOutcomeClassification.Success;

        if (unexpectedException)
            return SyncOperationOutcomeClassification.RetryableFailure;

        if (IsValidationFailure(result, operation))
            return SyncOperationOutcomeClassification.ValidationFailure;

        if (IsNonRetryableFailure(result, operation))
            return SyncOperationOutcomeClassification.NonRetryableFailure;

        return SyncOperationOutcomeClassification.RetryableFailure;
    }

    public static bool IsMixedBatch(int successCount, int failureCount, int conflictCount) =>
        (failureCount > 0 || conflictCount > 0) && successCount > 0;

    private static bool IsValidationFailure(OpResultDto result, OutboxOperationDto operation)
    {
        if (string.IsNullOrWhiteSpace(result.Message))
            return false;

        var msg = result.Message;
        if (msg.Contains("Missing required", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Invalid ", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("payload", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("must be", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("not found in inventory", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Invalid user token", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(operation.Type, "CreateOrder", StringComparison.Ordinal)
            || string.Equals(operation.Type, "FinalizeOrder", StringComparison.Ordinal)
            || string.Equals(operation.Type, "CashDrop", StringComparison.Ordinal)
            || string.Equals(operation.Type, "AdjustInventory", StringComparison.Ordinal)
            || string.Equals(operation.Type, "RecordWastage", StringComparison.Ordinal))
        {
            return !result.Success && !result.Conflict;
        }

        return false;
    }

    private static bool IsNonRetryableFailure(OpResultDto result, OutboxOperationDto operation)
    {
        if (string.Equals(result.Message, "Operation not supported", StringComparison.Ordinal)
            || result.Message?.StartsWith("Unknown operation type:", StringComparison.Ordinal) == true
            || result.Message?.Contains("operationId already recorded for a different operation type", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }
}
