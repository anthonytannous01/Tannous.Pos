namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Bounded operational fragility classification (operator wording).</summary>
public enum OperationalFragilityType
{
    DependencyConcentration,
    ContainmentInstability,
    EscalationRecurrence,
    RecoveryBrittleness,
    StabilizationWeakness,
    TopologyCollapseSensitivity
}
