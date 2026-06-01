namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Internal server-side classification for sync push operation outcomes (logging/governance only; not on mobile wire).
/// </summary>
public enum SyncOperationOutcomeClassification
{
    Success,
    ReplayShortCircuited,
    ValidationFailure,
    Conflict,
    PartialBatchRisk,
    PlaceholderOperation,
    RetryableFailure,
    NonRetryableFailure
}
