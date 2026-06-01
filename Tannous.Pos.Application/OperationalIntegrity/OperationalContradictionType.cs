namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Deterministic contradiction classification between operational interpretations.</summary>
public enum OperationalContradictionType
{
    RecoverySimulationMismatch = 0,
    EscalationRecoveryConflict = 1,
    PlaybookRecoveryDivergence = 2,
    DominantAreaConflict = 3,
    NarrativeContradiction = 4,
    PropagationRecoveryConflict = 5
}
