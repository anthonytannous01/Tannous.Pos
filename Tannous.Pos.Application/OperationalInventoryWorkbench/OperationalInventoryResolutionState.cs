namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

public enum OperationalInventoryResolutionState
{
    ReadyForOperatorReview = 0,
    StabilizationInProgress = 1,
    BlockedByReplayPressure = 2,
    BlockedByProtectiveMode = 3,
    ManualReconciliationRecommended = 4
}
