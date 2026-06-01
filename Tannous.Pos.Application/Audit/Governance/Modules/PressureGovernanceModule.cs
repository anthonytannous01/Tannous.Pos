namespace Tannous.Pos.Application.Audit.Governance.Modules;

public sealed class PressureGovernanceModule : IOperationalGovernanceModule
{
    public static PressureGovernanceModule Instance { get; } = new();

    public string DomainName => "Pressure";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = ["Core"];

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [
        nameof(OperationalPressureGovernanceProjectionBuilder),
        nameof(OperationalPressureStabilizationBuilder)
    ];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } =
    [nameof(OperationalPressureExplainabilityBuilder)];

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [
        nameof(OperationalPressureConvergenceClassifier),
        nameof(OperationalPressureRecoveryWindowClassifier)
    ];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } =
    [nameof(OperationalPressureGovernance)];
}
