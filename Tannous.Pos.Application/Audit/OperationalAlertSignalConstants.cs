namespace Tannous.Pos.Application.Audit;

/// <summary>Thresholds for heuristic alert signal derivation (visibility only).</summary>
public static class OperationalAlertSignalConstants
{
    public const int InventoryDriftWarningThreshold = 1;
    public const int InventoryDriftCriticalThreshold = 5;
    public const int LifecycleConflictWarningThreshold = 3;
    public const int LifecycleConflictCriticalThreshold = 10;
    public const int ConflictEscalationWarningThreshold = 15;
    public const int ConflictEscalationCriticalThreshold = 40;
}
