namespace Tannous.Pos.Application.Audit.Governance.Modules;

/// <summary>Pressure convergence and lifecycle stabilization domain (depends on Core + Pressure outputs).</summary>
public sealed class ConvergenceGovernanceModule : IOperationalGovernanceModule
{
    public static ConvergenceGovernanceModule Instance { get; } = new();

    public string DomainName => "Convergence";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = ["Core", "Pressure", "Consistency"];

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [nameof(OperationalPressureGovernanceProjectionBuilder)];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } =
    [nameof(OperationalPressureExplainabilityBuilder)];

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [nameof(OperationalPressureConvergenceClassifier)];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } =
    [nameof(OperationalPressureGovernance)];
}
