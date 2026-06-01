namespace Tannous.Pos.Application.Audit;

/// <summary>TTL and stale-risk thresholds for operational diagnostics cache (reporting-only; no hard budgets).</summary>
public static class OperationalDiagnosticsCacheConstants
{
    public static string ResilienceMetricsCacheKey => OperationalDiagnosticsCacheKeyFactory.BuildResilienceGlobal();
    public static string ReconciliationSummaryCacheKey => OperationalDiagnosticsCacheKeyFactory.BuildReconciliationGlobal();
    public static string IncidentGroupsCacheKey => OperationalDiagnosticsCacheKeyFactory.BuildIncidentGlobal();
    public static string AlertSignalsCacheKey => OperationalDiagnosticsCacheKeyFactory.BuildAlertSignalsGlobal();
    public static string AlertSummaryCacheKey => OperationalDiagnosticsCacheKeyFactory.BuildAlertSummaryGlobal();

    public const int ResilienceMetricsTtlSeconds = 30;
    public const int ReconciliationSummaryTtlSeconds = 30;
    public const int IncidentGroupsTtlSeconds = 45;
    public const int IncidentSummaryTtlSeconds = 45;
    public const int AlertSignalsTtlSeconds = 30;
    public const int AlertSummaryTtlSeconds = 30;
    public const int ForensicSnapshotSummaryTtlSeconds = 15;

    public const double AgingThresholdPercent = 0.5;
    public const double NearExpiryThresholdPercent = 0.9;

    public const int MaxCacheEntryCount = 64;

    public static TimeSpan GetTtlForCategory(string category) =>
        category switch
        {
            OperationalDiagnosticsCacheCategories.ResilienceMetrics =>
                TimeSpan.FromSeconds(ResilienceMetricsTtlSeconds),
            OperationalDiagnosticsCacheCategories.ReconciliationSummary =>
                TimeSpan.FromSeconds(ReconciliationSummaryTtlSeconds),
            OperationalDiagnosticsCacheCategories.IncidentGroups =>
                TimeSpan.FromSeconds(IncidentGroupsTtlSeconds),
            OperationalDiagnosticsCacheCategories.IncidentSummary =>
                TimeSpan.FromSeconds(IncidentSummaryTtlSeconds),
            OperationalDiagnosticsCacheCategories.AlertSignals =>
                TimeSpan.FromSeconds(AlertSignalsTtlSeconds),
            OperationalDiagnosticsCacheCategories.AlertSummary =>
                TimeSpan.FromSeconds(AlertSummaryTtlSeconds),
            OperationalDiagnosticsCacheCategories.ForensicSnapshotSummary =>
                TimeSpan.FromSeconds(ForensicSnapshotSummaryTtlSeconds),
            _ => TimeSpan.FromSeconds(ResilienceMetricsTtlSeconds)
        };
}
