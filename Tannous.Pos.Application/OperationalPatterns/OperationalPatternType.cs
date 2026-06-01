namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic operational pattern classification.</summary>
public enum OperationalPatternType
{
    EscalationCycle = 0,
    StabilizationArchetype = 1,
    RecoveryConvergence = 2,
    PropagationSequence = 3,
    VolatilityCycle = 4,
    ContainmentRecovery = 5,
    DriftCascade = 6,
    CrossDomainInstability = 7
}
