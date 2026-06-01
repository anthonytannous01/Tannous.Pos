namespace Tannous.Pos.Application.Audit.Governance.Modules;

public sealed class SurvivabilityGovernanceModule : IOperationalGovernanceModule
{
    public static SurvivabilityGovernanceModule Instance { get; } = new();

    public string DomainName => "Survivability";

    public IReadOnlyList<string> AllowedDependencyDomains { get; } = ["Core"];

    public IReadOnlyList<string> ProjectionBuilderTypes { get; } =
    [nameof(OperationalCacheScopeSurvivabilityBuilder)];

    public IReadOnlyList<string> ExplainabilityContributorTypes { get; } = Array.Empty<string>();

    public IReadOnlyList<string> ClassifierTypes { get; } =
    [
        nameof(OperationalCacheSurvivabilityClassifier),
        nameof(OperationalCacheDegradationClassifier)
    ];

    public IReadOnlyList<string> GovernanceConstantTypes { get; } = Array.Empty<string>();
}
