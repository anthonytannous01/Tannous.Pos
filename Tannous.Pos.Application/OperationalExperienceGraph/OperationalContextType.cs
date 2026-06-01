namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Deterministic operational context classification.</summary>
public enum OperationalContextType
{
    ReplayInstability = 0,
    RuntimeContainment = 1,
    RecoveryVerification = 2,
    IncidentInvestigation = 3,
    StabilizationGuidance = 4,
    OperationalOverview = 5
}
