namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Operator-facing incident lifecycle state (advisory; not persisted).</summary>
public enum OperationalIncidentState
{
    Active = 0,
    Stabilizing = 1,
    Recovering = 2,
    Escalating = 3,
    Recurring = 4,
    Resolved = 5
}
