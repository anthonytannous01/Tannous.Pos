namespace Tannous.Pos.Application.Audit;

/// <summary>Internal degraded-mode classifications for operator diagnostics (informational only).</summary>
public static class OperationalDegradedModeTypes
{
    public const string Normal = "Normal";
    public const string ElevatedQueryPressure = "ElevatedQueryPressure";
    public const string ReconciliationPressure = "ReconciliationPressure";
    public const string ExportPressure = "ExportPressure";
    public const string AuditPersistencePressure = "AuditPersistencePressure";
    public const string ReplayStormRisk = "ReplayStormRisk";
}
