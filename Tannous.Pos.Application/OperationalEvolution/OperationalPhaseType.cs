namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational evolution phase classification.</summary>
public enum OperationalPhaseType
{
    Escalation = 0,
    Stabilization = 1,
    Recovery = 2,
    Containment = 3,
    Convergence = 4,
    Fragmentation = 5
}
