namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Overall operational equilibrium posture.</summary>
public enum OperationalEquilibriumState
{
    Balanced,
    StabilizationDominant,
    EscalationStrained,
    RecoveryImbalanced,
    Fragmented,
    Overloaded
}
