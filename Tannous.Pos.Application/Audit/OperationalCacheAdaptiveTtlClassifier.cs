namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Bounded adaptive TTL governance (never exceeds configured static TTL; floor enforced per category).
/// GOVERNANCE / NON-GOAL: no ML, no prediction engines, no external metrics.
/// </summary>
public static class OperationalCacheAdaptiveTtlClassifier
{
    public const int ResilienceMinimumTtlSeconds = 5;
    public const int StandardMinimumTtlSeconds = 10;

    public static OperationalCacheTtlMode ClassifyTtlMode(OperationalCacheAdaptivePressureSignals signals)
    {
        if (signals.ReplayStormRiskIndicated || signals.ForensicExportTruncated)
            return OperationalCacheTtlMode.BypassPreferred;

        if (signals.ReconciliationBacklogElevated
            && (signals.QueryDateRangeClamped || signals.QueryPageSizeClamped))
            return OperationalCacheTtlMode.BypassPreferred;

        var count = signals.ActiveSignalCount();
        if (count >= 2)
            return OperationalCacheTtlMode.Minimal;

        if (count == 1)
        {
            if (signals.QueryDateRangeClamped || signals.QueryPageSizeClamped)
                return OperationalCacheTtlMode.Reduced;

            return OperationalCacheTtlMode.Minimal;
        }

        return OperationalCacheTtlMode.Normal;
    }

    public static TimeSpan GetAdaptiveTtl(string category, OperationalCacheTtlMode mode)
    {
        var baseTtl = OperationalDiagnosticsCacheConstants.GetTtlForCategory(category);
        var factor = mode switch
        {
            OperationalCacheTtlMode.Normal => 1.0,
            OperationalCacheTtlMode.Reduced => 0.5,
            OperationalCacheTtlMode.Minimal => 0.25,
            OperationalCacheTtlMode.BypassPreferred => 0.25,
            _ => 1.0
        };

        var seconds = (int)Math.Ceiling(baseTtl.TotalSeconds * factor);
        seconds = Math.Max(GetMinimumTtlSeconds(category), seconds);
        var adaptive = TimeSpan.FromSeconds(seconds);

        if (adaptive > baseTtl)
            return baseTtl;

        return adaptive;
    }

    public static TimeSpan GetEffectiveTtl(
        string category,
        OperationalCacheAdaptivePressureSignals signals,
        out OperationalCacheTtlMode mode)
    {
        mode = ClassifyTtlMode(signals);
        return GetAdaptiveTtl(category, mode);
    }

    public static int GetMinimumTtlSeconds(string category) =>
        category == OperationalDiagnosticsCacheCategories.ResilienceMetrics
            ? ResilienceMinimumTtlSeconds
            : StandardMinimumTtlSeconds;

    public static bool IsTtlReduced(string category, OperationalCacheTtlMode mode)
    {
        if (mode == OperationalCacheTtlMode.Normal)
            return false;

        var baseSeconds = (int)OperationalDiagnosticsCacheConstants.GetTtlForCategory(category).TotalSeconds;
        var effectiveSeconds = (int)GetAdaptiveTtl(category, mode).TotalSeconds;
        return effectiveSeconds < baseSeconds;
    }

    /// <summary>Further bounded TTL shrink under heuristic cache pressure (never below category floor).</summary>
    public static TimeSpan ApplyCachePressureSeverity(
        TimeSpan ttl,
        string category,
        OperationalCachePressureSeverity severity)
    {
        if (severity == OperationalCachePressureSeverity.Nominal)
            return ttl;

        var factor = severity switch
        {
            OperationalCachePressureSeverity.Elevated => 0.9,
            OperationalCachePressureSeverity.High => 0.85,
            OperationalCachePressureSeverity.Critical => 0.75,
            _ => 1.0
        };

        var seconds = (int)Math.Ceiling(ttl.TotalSeconds * factor);
        seconds = Math.Max(GetMinimumTtlSeconds(category), seconds);
        var adjusted = TimeSpan.FromSeconds(seconds);
        var baseTtl = OperationalDiagnosticsCacheConstants.GetTtlForCategory(category);
        return adjusted > baseTtl ? baseTtl : adjusted;
    }
}
