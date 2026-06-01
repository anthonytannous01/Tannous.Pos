namespace Tannous.Pos.Application.Audit;

/// <summary>Bounded explainability codes for invalidation governance (deterministic ordering).</summary>
public static class OperationalCacheInvalidationExplainabilityBuilder
{
    public static IReadOnlyList<string> Bound(IEnumerable<string> items) =>
        OperationalGovernanceExplainabilityComposer.Compose(
            items,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Invalidation);

    public static string NormalizeCode(string code) =>
        OperationalGovernanceExplainabilityComposer.NormalizeCode(
            code,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Invalidation);

    public static IReadOnlyList<string> BuildInvalidationReasonCodes(
        OperationalCacheInvalidationSeverity severity,
        OperationalCacheFreshnessRecoveryState recovery,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        int activeScopedKeyCount,
        double scopeChurnRatio)
    {
        var codes = new List<string> { $"Invalidation{severity}" };

        if (telemetry.CrossCategoryInvalidations > 0)
            codes.Add("CrossCategoryCascade");

        if (telemetry.ScopedInvalidationRecoveries > 0)
            codes.Add("ScopedRecoveryObserved");

        if (telemetry.FreshnessRecoveryCount > 0)
            codes.Add("FrequentFreshnessRecovery");

        if (severity >= OperationalCacheInvalidationSeverity.High)
            codes.Add("InvalidationPressureElevated");

        if (activeScopedKeyCount >= OperationalDiagnosticsCacheConstants.MaxCacheEntryCount / 2)
            codes.Add("ScopeSaturationDetected");

        if (scopeChurnRatio >= OperationalCacheInvalidationGovernance.ElevatedScopeChurnRatio)
            codes.Add("HighScopedInvalidationChurn");

        if (recovery == OperationalCacheFreshnessRecoveryState.Unstable)
            codes.Add("FreshnessUnstable");

        return Bound(codes);
    }
}
