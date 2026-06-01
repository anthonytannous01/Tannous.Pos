namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Validates compile-time governance module dependency boundaries (reporting + architecture guardrails).
/// </summary>
public static class OperationalGovernanceDependencyRules
{
    public const int MaxCollaboratorsPerModule = 2;
    public const int MaxCrossDomainClassifierReferences = 0;

    /// <summary>Cross-domain classifier usage forbidden in domain projection builders (except Core shared types).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ForbiddenCrossModuleClassifierReferences { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Invalidation"] = ["OperationalPressureConvergenceClassifier", "OperationalCacheRecoveryContainmentClassifier"],
            ["Pressure"] = ["OperationalCacheInvalidationSeverityClassifier", "OperationalCacheFreshnessRecoveryClassifier"],
            ["Survivability"] = ["OperationalPressureConvergenceClassifier", "OperationalCacheInvalidationSeverityClassifier"],
            ["Consistency"] = ["OperationalPressureRecoveryWindowClassifier"],
            ["Convergence"] = ["OperationalCacheInvalidationSeverityClassifier", "OperationalCacheFreshnessRecoveryClassifier"]
        };

    public static bool HasCircularDependencies(IReadOnlyDictionary<string, IReadOnlyList<string>> graph)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in graph.Keys)
        {
            if (Visit(node, graph, visiting, visited))
                return true;
        }

        return false;
    }

    public static int ComputeModuleCouplingScore(IReadOnlyDictionary<string, IReadOnlyList<string>> graph) =>
        graph.Values.Sum(deps => deps.Count);

    private static bool Visit(
        string node,
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(node))
            return false;

        if (!visiting.Add(node))
            return true;

        if (graph.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (Visit(dep, graph, visiting, visited))
                    return true;
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        return false;
    }
}
