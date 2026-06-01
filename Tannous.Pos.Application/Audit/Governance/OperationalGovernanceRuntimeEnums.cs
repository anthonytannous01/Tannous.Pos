namespace Tannous.Pos.Application.Audit.Governance;

public enum OperationalGovernanceExecutionState
{
    Healthy,
    Constrained,
    Saturated,
    Failsafe
}

public enum OperationalGovernanceBudgetPressure
{
    Nominal,
    Elevated,
    High,
    Critical
}

public enum OperationalGovernanceProjectionComplexity
{
    Minimal,
    Moderate,
    Heavy,
    Extreme
}

public enum OperationalGovernanceTelemetrySaturationLevel
{
    Nominal,
    Elevated,
    Saturated
}
