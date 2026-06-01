namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Static runtime governance ceilings (process-local; advisory only).
/// GOVERNANCE: no business-path throttling; projection-time bounded enforcement.
/// </summary>
public static class OperationalGovernanceRuntimeBudget
{
    public const int MaxExplainabilitySignals = 8;
    public const int MaxGovernanceRecommendations = 8;
    public const int MaxProjectionCollaborators = 8;
    public const int MaxPipelineDepth = 8;
    public const int MaxTelemetryCategories = 12;
    public const int MaxStaleRiskProjections = 64;
    public const int MaxSurvivabilityScopeEntries = 64;
    public const int MaxConsistencySignals = 8;
    public const int FailsafeExplainabilityCap = 4;
    public const int FailsafeRecommendationCap = 3;

    public static int GetEffectiveExplainabilityCap(
        OperationalGovernanceExecutionState executionState,
        OperationalGovernanceProfile profile)
    {
        var profileCap = OperationalGovernanceProfileSettings.GetExplainabilityCap(profile);
        if (executionState == OperationalGovernanceExecutionState.Failsafe)
            return Math.Min(profileCap, FailsafeExplainabilityCap);

        return profileCap;
    }

    public static int GetEffectiveRecommendationCap(OperationalGovernanceExecutionState executionState)
    {
        if (executionState == OperationalGovernanceExecutionState.Failsafe)
            return FailsafeRecommendationCap;

        return MaxGovernanceRecommendations;
    }

    public static IReadOnlyList<T> ClampOrdered<T>(
        IEnumerable<T> items,
        int maxCount) =>
        items.Take(Math.Max(0, maxCount)).ToList();

    public static IReadOnlyList<string> ClampExplainabilityOrdered(
        IEnumerable<string> items,
        int maxCount) =>
        items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .OrderBy(s => s, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .Take(Math.Max(0, maxCount))
            .ToList();
}
