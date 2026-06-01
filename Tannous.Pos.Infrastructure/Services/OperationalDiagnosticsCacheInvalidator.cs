using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>Best-effort targeted invalidation for operational diagnostics caches only.</summary>
public sealed class OperationalDiagnosticsCacheInvalidator : IOperationalDiagnosticsCacheInvalidator
{
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly ILogger<OperationalDiagnosticsCacheInvalidator> _logger;

    public OperationalDiagnosticsCacheInvalidator(
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger<OperationalDiagnosticsCacheInvalidator> logger)
    {
        _cache = cache;
        _telemetry = telemetry;
        _logger = logger;
    }

    public void InvalidateAfterReconciliationWorkflow()
    {
        TryInvalidate(
            "reconciliation workflow transition",
            () =>
            {
                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
                    OperationalDiagnosticsCacheCategories.ReconciliationSummary);
                _cache.RemoveByPrefix($"{OperationalDiagnosticsCacheKeyConstants.ReconciliationDomain}:");
                _cache.RemoveByPrefix($"{OperationalDiagnosticsCacheKeyConstants.AlertSignalsSegment}:");
                _cache.RemoveByPrefix($"{OperationalDiagnosticsCacheKeyConstants.AlertSummarySegment}:");
                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey,
                    OperationalDiagnosticsCacheCategories.IncidentGroups);
                _cache.RemoveByPrefix($"{OperationalDiagnosticsCacheKeyConstants.IncidentDomain}:");
                NoteCrossCategoryInvalidation(4);
            });
    }

    public void InvalidateAfterConflictRecorded(string conflictType, string? deviceId, string? operationId)
    {
        TryInvalidate(
            $"conflict recorded ({conflictType})",
            () =>
            {
                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
                    OperationalDiagnosticsCacheCategories.ReconciliationSummary);
                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey,
                    OperationalDiagnosticsCacheCategories.IncidentGroups);

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    _cache.RemoveByScope(
                        OperationalDiagnosticsCacheCategories.IncidentGroups,
                        OperationalDiagnosticsCacheScopes.Device,
                        deviceId);
                }

                if (!string.IsNullOrWhiteSpace(operationId))
                {
                    _cache.RemoveByScope(
                        OperationalDiagnosticsCacheCategories.IncidentGroups,
                        OperationalDiagnosticsCacheScopes.Operation,
                        operationId);
                }

                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.AlertSignalsCacheKey,
                    OperationalDiagnosticsCacheCategories.AlertSignals);
                _cache.Remove(
                    OperationalDiagnosticsCacheConstants.AlertSummaryCacheKey,
                    OperationalDiagnosticsCacheCategories.AlertSummary);

                if (IsReplayPressureConflict(conflictType) || IsLifecycleConflict(conflictType))
                {
                    _cache.Remove(
                        OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                        OperationalDiagnosticsCacheCategories.ResilienceMetrics);
                    NoteCrossCategoryInvalidation(4);
                }
                else
                {
                    NoteCrossCategoryInvalidation(3);
                }
            });
    }

    public void InvalidateAllDiagnosticsCaches() =>
        TryInvalidate("remove all diagnostics caches", () => _cache.RemoveAllDiagnosticsCaches());

    private void TryInvalidate(string reason, Action invalidate)
    {
        try
        {
            invalidate();
            NoteInvalidationPressureIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Operational cache invalidation failure: best-effort invalidation failed. Reason={Reason}",
                reason);
        }
    }

    private void NoteCrossCategoryInvalidation(int categoriesAffected) =>
        _telemetry.RecordCrossCategoryInvalidation(categoriesAffected);

    private void NoteInvalidationPressureIfNeeded()
    {
        var snapshot = _telemetry.GetSnapshot();

        if (snapshot.CrossCategoryInvalidations > 0)
            _telemetry.RecordPropagationDetection();

        if (snapshot.TotalInvalidations >= OperationalCacheConsistencyGovernance.RecoveryWindowExtensionInvalidationThreshold)
            _telemetry.RecordRecoveryWindowExtension();

        if (snapshot.TotalInvalidations < OperationalCacheInvalidationGovernance.HighInvalidationChurnThreshold)
            return;

        _telemetry.RecordInvalidationPressureEscalation();
        _telemetry.RecordContainmentEscalation();

        if (snapshot.CrossCategoryInvalidations > 0 && snapshot.ScopedInvalidations == 0)
            _telemetry.RecordInvalidationDrift();

        var total = snapshot.TotalHits + snapshot.TotalMisses;
        if (total > 0)
        {
            var hitRatio = (double)snapshot.TotalHits / total;
            if (hitRatio < OperationalCacheConsistencyGovernance.LowHitRatioThreshold
                && snapshot.TotalBypasses > 0)
                _telemetry.RecordConsistencyConfidenceDrop();
        }
    }

    private static bool IsReplayPressureConflict(string conflictType) =>
        conflictType.Contains("ReplayMismatch", StringComparison.OrdinalIgnoreCase);

    private static bool IsLifecycleConflict(string conflictType) =>
        conflictType.Contains("Lifecycle", StringComparison.OrdinalIgnoreCase)
        || conflictType.Contains("StaleOffline", StringComparison.OrdinalIgnoreCase);
}
