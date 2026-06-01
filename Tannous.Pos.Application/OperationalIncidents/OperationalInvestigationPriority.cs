namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Operator-facing investigation priority band.</summary>
public enum OperationalInvestigationPriority
{
    Critical = 0,
    High = 1,
    Elevated = 2,
    Moderate = 3,
    Monitoring = 4,
    Stable = 5
}
