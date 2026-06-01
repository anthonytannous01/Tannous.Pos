namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Advisory execution budget band for governance projections (process-local).</summary>
public enum OperationalGovernanceExecutionBudgetState
{
    Nominal,
    Elevated,
    Constrained,
    Critical
}

/// <summary>Coarse projection build duration band (not per-stage profiling).</summary>
public enum OperationalGovernanceProjectionTimingBand
{
    Fast,
    Moderate,
    Slow,
    Elevated
}

/// <summary>Advisory production readiness classification (non-authoritative).</summary>
public enum OperationalGovernanceProductionReadinessState
{
    DevelopmentReady,
    IntegrationReady,
    OperationallyStable,
    GovernanceSaturated
}
