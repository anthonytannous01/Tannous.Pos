using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Infrastructure.Services;

internal static class OperationalCacheAdaptivePressureSignalBuilder
{
    private static readonly string[] BacklogSeveritiesElevated = { "Elevated", "High" };

    public static OperationalCacheAdaptivePressureSignals FromPressureState(
        IOperationalResiliencePressureState pressureState) =>
        OperationalCacheAdaptivePressureSignals.FromPressureState(pressureState);

    public static OperationalCacheAdaptivePressureSignals ForResilience(
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCache cache,
        Func<OperationalResilienceMetricsSnapshot, bool> isReplayStormRisk)
    {
        var signals = FromPressureState(pressureState);
        if (cache.TryGetEnvelope<OperationalResilienceMetricsSnapshot>(
                OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                OperationalDiagnosticsCacheCategories.ResilienceMetrics,
                out var cached)
            && cached != null)
            return WithReplayStorm(signals, isReplayStormRisk(cached.Value));

        return signals;
    }

    public static OperationalCacheAdaptivePressureSignals ForReconciliation(
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCache cache,
        Func<OperationalResilienceMetricsSnapshot, bool> isReplayStormRisk,
        Func<ReconciliationSummaryDto, string> classifyBacklogSeverity)
    {
        var signals = FromPressureState(pressureState);

        if (cache.TryGetEnvelope<OperationalResilienceMetricsSnapshot>(
                OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                OperationalDiagnosticsCacheCategories.ResilienceMetrics,
                out var resilienceCached)
            && resilienceCached != null
            && isReplayStormRisk(resilienceCached.Value))
            signals = WithReplayStorm(signals, replayStormRiskIndicated: true);

        if (cache.TryGetEnvelope<ReconciliationSummaryDto>(
                OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
                OperationalDiagnosticsCacheCategories.ReconciliationSummary,
                out var summaryCached)
            && summaryCached != null)
        {
            var severity = classifyBacklogSeverity(summaryCached.Value);
            if (BacklogSeveritiesElevated.Contains(severity, StringComparer.OrdinalIgnoreCase))
                signals = WithBacklogElevated(signals, reconciliationBacklogElevated: true);
        }

        return signals;
    }

    public static OperationalCacheAdaptivePressureSignals ForIncident(
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCache cache,
        Func<OperationalResilienceMetricsSnapshot, bool> isReplayStormRisk)
    {
        var signals = FromPressureState(pressureState);
        if (cache.TryGetEnvelope<OperationalResilienceMetricsSnapshot>(
                OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                OperationalDiagnosticsCacheCategories.ResilienceMetrics,
                out var cached)
            && cached != null)
            return WithReplayStorm(signals, isReplayStormRisk(cached.Value));

        return signals;
    }

    private static OperationalCacheAdaptivePressureSignals WithReplayStorm(
        OperationalCacheAdaptivePressureSignals signals,
        bool replayStormRiskIndicated) =>
        new()
        {
            QueryDateRangeClamped = signals.QueryDateRangeClamped,
            QueryPageSizeClamped = signals.QueryPageSizeClamped,
            ForensicExportTruncated = signals.ForensicExportTruncated,
            ReplayStormRiskIndicated = replayStormRiskIndicated,
            ReconciliationBacklogElevated = signals.ReconciliationBacklogElevated
        };

    private static OperationalCacheAdaptivePressureSignals WithBacklogElevated(
        OperationalCacheAdaptivePressureSignals signals,
        bool reconciliationBacklogElevated) =>
        new()
        {
            QueryDateRangeClamped = signals.QueryDateRangeClamped,
            QueryPageSizeClamped = signals.QueryPageSizeClamped,
            ForensicExportTruncated = signals.ForensicExportTruncated,
            ReplayStormRiskIndicated = signals.ReplayStormRiskIndicated,
            ReconciliationBacklogElevated = reconciliationBacklogElevated
        };
}