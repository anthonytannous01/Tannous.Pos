namespace Tannous.Pos.Application.Audit;

/// <summary>Domain segments for structured operational diagnostics cache keys.</summary>
public static class OperationalDiagnosticsCacheKeyConstants
{
    public const string ResilienceDomain = "resilience";
    public const string ReconciliationDomain = "reconciliation";
    public const string IncidentDomain = "incident";
    public const string AlertSignalsSegment = "signals";
    public const string AlertSummarySegment = "summary";

    public const int MaxKeyLength = 128;

    /// <summary>Prefix for RemoveByPrefix targeting all operational diagnostics cache entries.</summary>
    public const string DiagnosticsKeyPrefix = "op-diag";
}
