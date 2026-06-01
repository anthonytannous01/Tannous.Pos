namespace Tannous.Pos.Application.Audit.Governance.Modules;

/// <summary>Shared governance foundation (thresholds, explainability, stale-risk, composition).</summary>
public sealed class CoreGovernanceModule : IOperationalGovernanceModule
{
    public static CoreGovernanceModule Instance { get; } = new();

    public string DomainName => "Core";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = Array.Empty<string>();

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [
        nameof(OperationalGovernanceStaleRiskProjectionBuilder)
    ];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } =
    [
        nameof(OperationalGovernanceExplainabilityComposer),
        nameof(OperationalCacheExplainabilityBuilder)
    ];

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [
        nameof(OperationalGovernanceThresholdEvaluator),
        nameof(OperationalGovernanceClassificationNormalizer),
        nameof(OperationalCacheStabilityClassifier),
        nameof(OperationalCacheCardinalityClassifier),
        nameof(OperationalCachePressureClassifier)
    ];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } =
    [
        nameof(OperationalGovernanceSurfaceBudget),
        nameof(OperationalCacheGovernanceFinalizationGovernance)
    ];
}
