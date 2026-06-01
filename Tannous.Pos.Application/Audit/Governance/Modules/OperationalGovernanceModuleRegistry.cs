namespace Tannous.Pos.Application.Audit.Governance.Modules;

/// <summary>Compile-time registry of governance domain modules (static; no reflection/discovery).</summary>
public static class OperationalGovernanceModuleRegistry
{
    public static IReadOnlyList<IOperationalGovernanceModule> All { get; } =
    [
        CoreGovernanceModule.Instance,
        PressureGovernanceModule.Instance,
        InvalidationGovernanceModule.Instance,
        SurvivabilityGovernanceModule.Instance,
        ConsistencyGovernanceModule.Instance,
        ConvergenceGovernanceModule.Instance
    ];

    public static IOperationalGovernanceModule Get(string domainName) =>
        All.First(m => string.Equals(m.DomainName, domainName, StringComparison.Ordinal));

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DependencyGraph() =>
        All.ToDictionary(
            m => m.DomainName,
            m => m.AllowedDependencyDomains,
            StringComparer.Ordinal);
}
