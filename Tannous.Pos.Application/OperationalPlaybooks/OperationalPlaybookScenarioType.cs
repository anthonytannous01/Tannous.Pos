namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Deterministic operational playbook scenario classification.</summary>
public enum OperationalPlaybookScenarioType
{
    ReplayStabilization = 0,
    RuntimeContainment = 1,
    InventoryDriftStabilization = 2,
    ReconciliationRecovery = 3,
    IncidentContinuity = 4,
    RecoveryAcceleration = 5,
    CrossDomainMonitoring = 6
}
