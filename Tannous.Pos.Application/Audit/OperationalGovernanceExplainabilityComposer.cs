namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Unified explainability composition: deterministic ordering, deduplication, bounded size.
/// GOVERNANCE: domain builders contribute signals; this type enforces limits only.
/// </summary>
public static class OperationalGovernanceExplainabilityComposer
{
    public enum ExplainabilityProfile
    {
        CacheGovernance,
        Consistency,
        Invalidation,
        Pressure
    }

    public static IReadOnlyList<string> Compose(IEnumerable<string> items, ExplainabilityProfile profile)
    {
        var (maxItems, maxLength, orderDeterministically) = GetProfileSettings(profile);

        var query = items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => NormalizeCode(s, maxLength))
            .Distinct(StringComparer.Ordinal);

        if (orderDeterministically)
            query = query.OrderBy(s => s, StringComparer.Ordinal);

        return query.Take(maxItems).ToList();
    }

    /// <summary>Runtime-aware bounded composition with explicit cap (failsafe/saturation).</summary>
    public static IReadOnlyList<string> ComposeWithRuntimeCap(
        IEnumerable<string> items,
        int maxItems,
        bool orderDeterministically = true)
    {
        var maxLength = OperationalCacheGovernanceFinalizationGovernance.MaxReasonCodeLength;
        var query = items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => NormalizeCode(s, maxLength))
            .Distinct(StringComparer.Ordinal);

        if (orderDeterministically)
            query = query.OrderBy(s => s, StringComparer.Ordinal);

        return query.Take(Math.Max(0, maxItems)).ToList();
    }

    public static string NormalizeCode(string code, ExplainabilityProfile profile)
    {
        var (_, maxLength, _) = GetProfileSettings(profile);
        return NormalizeCode(code, maxLength);
    }

    public static string NormalizeCode(string code, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var trimmed = code.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static (int MaxItems, int MaxLength, bool OrderDeterministically) GetProfileSettings(
        ExplainabilityProfile profile) =>
        profile switch
        {
            ExplainabilityProfile.CacheGovernance => (
                OperationalCacheGovernanceFinalizationGovernance.MaxExplainabilityItems,
                OperationalCacheGovernanceFinalizationGovernance.MaxReasonCodeLength,
                false),
            ExplainabilityProfile.Consistency => (
                OperationalCacheConsistencyGovernance.MaxExplainabilityItems,
                OperationalCacheConsistencyGovernance.MaxReasonCodeLength,
                true),
            ExplainabilityProfile.Invalidation => (
                OperationalCacheInvalidationGovernance.MaxReasonCodes,
                OperationalCacheInvalidationGovernance.MaxReasonCodeLength,
                true),
            ExplainabilityProfile.Pressure => (
                OperationalPressureGovernance.MaxExplainabilityItems,
                OperationalPressureGovernance.MaxReasonCodeLength,
                true),
            _ => (
                OperationalCacheGovernanceFinalizationGovernance.MaxExplainabilityItems,
                OperationalCacheGovernanceFinalizationGovernance.MaxReasonCodeLength,
                true)
        };
}
