namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational state transition classification.</summary>
public enum OperationalTransitionType
{
    RecoveryProgression = 0,
    EscalationProgression = 1,
    StabilizationShift = 2,
    IntegrityShift = 3,
    NarrativeShift = 4,
    PhaseTransition = 5
}
