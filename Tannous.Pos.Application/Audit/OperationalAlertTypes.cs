namespace Tannous.Pos.Application.Audit;

/// <summary>Internal alert signal classifications (heuristic; not persisted; not delivered).</summary>
public static class OperationalAlertTypes
{
    public const string ReplayStormRisk = "ReplayStormRisk";
    public const string AuditPersistencePressure = "AuditPersistencePressure";
    public const string InventoryDriftEscalation = "InventoryDriftEscalation";
    public const string CascadingOperationalPressure = "CascadingOperationalPressure";
    public const string ReconciliationBacklog = "ReconciliationBacklog";
    public const string ConflictEscalation = "ConflictEscalation";
    public const string ExportTruncationPressure = "ExportTruncationPressure";
    public const string LifecycleConflictSpike = "LifecycleConflictSpike";
}
