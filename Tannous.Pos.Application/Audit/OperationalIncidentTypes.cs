namespace Tannous.Pos.Application.Audit;

/// <summary>Operational incident correlation types (computed dynamically; not persisted).</summary>
public static class OperationalIncidentTypes
{
    public const string ReplayIncident = "ReplayIncident";
    public const string ReconciliationIncident = "ReconciliationIncident";
    public const string SettlementInconsistencyIncident = "SettlementInconsistencyIncident";
    public const string InventoryDriftIncident = "InventoryDriftIncident";
    public const string ResiliencePressureIncident = "ResiliencePressureIncident";
    public const string ForensicSurvivabilityIncident = "ForensicSurvivabilityIncident";
    public const string CascadingDegradationIncident = "CascadingDegradationIncident";
}
