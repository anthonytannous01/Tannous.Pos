namespace Tannous.Pos.Application.Audit.Governance.Modules;

public sealed class InvalidationGovernanceModule : IOperationalGovernanceModule
{
    public static InvalidationGovernanceModule Instance { get; } = new();

    public string DomainName => "Invalidation";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = ["Core"];

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [nameof(OperationalCacheInvalidationProjectionBuilder)];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } =
    [nameof(OperationalCacheInvalidationExplainabilityBuilder)];

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [
        nameof(OperationalCacheInvalidationSeverityClassifier),
        nameof(OperationalCacheFreshnessRecoveryClassifier)
    ];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } =
    [nameof(OperationalCacheInvalidationGovernance)];
}
