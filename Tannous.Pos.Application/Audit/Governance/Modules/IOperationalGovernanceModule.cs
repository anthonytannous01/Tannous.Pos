namespace Tannous.Pos.Application.Audit.Governance.Modules;

/// <summary>Compile-time governance domain boundary descriptor (not runtime-discovered).</summary>
public interface IOperationalGovernanceModule
{
    string DomainName { get; }

    IReadOnlyList<string> AllowedDependencyDomains { get; }

    IReadOnlyList<string> ProjectionBuilderTypes { get; }

    IReadOnlyList<string> ExplainabilityContributorTypes { get; }

    IReadOnlyList<string> ClassifierTypes { get; }

    IReadOnlyList<string> GovernanceConstantTypes { get; }
}
