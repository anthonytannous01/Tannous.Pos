namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Bounded operational dependency classification (operator wording).</summary>
public enum OperationalDependencyType
{
    StabilizationDependency,
    EscalationDependency,
    PropagationDependency,
    RecoveryDependency,
    NavigationDependency,
    SequencingDependency
}
