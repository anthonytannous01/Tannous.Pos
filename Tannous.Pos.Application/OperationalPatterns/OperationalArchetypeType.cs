namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic stabilization archetype classification.</summary>
public enum OperationalArchetypeType
{
    ReplayEscalationCycle = 0,
    RuntimeContainmentRecovery = 1,
    InventoryDriftCascade = 2,
    ReconciliationVolatilityCycle = 3,
    RecoveryConvergence = 4,
    IncidentRecurrence = 5,
    OperationalVolatility = 6
}
