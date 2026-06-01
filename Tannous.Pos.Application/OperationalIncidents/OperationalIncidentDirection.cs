namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Operator-facing incident movement direction.</summary>
public enum OperationalIncidentDirection
{
    Improving = 0,
    Stable = 1,
    Degrading = 2,
    Escalating = 3,
    Converging = 4,
    Diverging = 5
}
