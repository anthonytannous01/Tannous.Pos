namespace Tannous.Pos.Application.Audit.Governance.Modules;

public sealed class ConsistencyGovernanceModule : IOperationalGovernanceModule
{
    public static ConsistencyGovernanceModule Instance { get; } = new();

    public string DomainName => "Consistency";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = ["Core", "Survivability"];

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [nameof(OperationalCacheConsistencyProjectionBuilder)];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } =
    [nameof(OperationalCacheConsistencyExplainabilityBuilder)];

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [
        nameof(OperationalCacheConsistencyConfidenceClassifier),
        nameof(OperationalCacheRecoveryContainmentClassifier),
        nameof(OperationalCacheRecoveryWindowClassifier)
    ];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } =
    [nameof(OperationalCacheConsistencyGovernance)];
}
