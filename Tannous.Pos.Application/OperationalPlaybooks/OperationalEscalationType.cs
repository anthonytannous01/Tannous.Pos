namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Deterministic escalation guidance classification.</summary>
public enum OperationalEscalationType
{
    PropagationEscalation = 0,
    RuntimeSurvivability = 1,
    IncidentEscalation = 2,
    RecoveryDivergence = 3,
    OperationalVolatility = 4
}
