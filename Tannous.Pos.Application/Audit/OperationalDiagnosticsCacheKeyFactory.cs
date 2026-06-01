using System.Text;
using System.Text.RegularExpressions;

namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Deterministic, sanitized cache keys for operational diagnostics summaries only.
/// GOVERNANCE: no raw payloads, query strings, user/session scopes, or unbounded cardinality.
/// </summary>
public static partial class OperationalDiagnosticsCacheKeyFactory
{
    public static string BuildResilienceGlobal() =>
        Build(OperationalDiagnosticsCacheKeyConstants.ResilienceDomain, OperationalDiagnosticsCacheScopes.Global);

    public static string BuildReconciliationGlobal() =>
        Build(OperationalDiagnosticsCacheKeyConstants.ReconciliationDomain, OperationalDiagnosticsCacheScopes.Global);

    public static string BuildIncidentGlobal() =>
        Build(OperationalDiagnosticsCacheKeyConstants.IncidentDomain, OperationalDiagnosticsCacheScopes.Global);

    public static string BuildIncidentDevice(string deviceId) =>
        Build(OperationalDiagnosticsCacheKeyConstants.IncidentDomain, OperationalDiagnosticsCacheScopes.Device, deviceId);

    public static string BuildIncidentOperation(string operationId) =>
        Build(OperationalDiagnosticsCacheKeyConstants.IncidentDomain, OperationalDiagnosticsCacheScopes.Operation, operationId);

    public static string BuildIncidentOrder(Guid orderId) =>
        Build(OperationalDiagnosticsCacheKeyConstants.IncidentDomain, OperationalDiagnosticsCacheScopes.Order, orderId.ToString("N"));

    public static string BuildAlertSignalsGlobal() =>
        Build(
            OperationalDiagnosticsCacheKeyConstants.AlertSignalsSegment,
            OperationalDiagnosticsCacheScopes.Global);

    public static string BuildAlertSummaryGlobal() =>
        Build(
            OperationalDiagnosticsCacheKeyConstants.AlertSummarySegment,
            OperationalDiagnosticsCacheScopes.Global);

    public static string Build(string domain, string scope, string? scopeId = null)
    {
        var key = scope == OperationalDiagnosticsCacheScopes.Global || string.IsNullOrWhiteSpace(scopeId)
            ? $"{domain}:{scope}"
            : $"{domain}:{scope}:{SanitizeScopeId(scopeId)}";

        return Truncate(key);
    }

    public static string SanitizeScopeId(string scopeId)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            return "unknown";

        var trimmed = scopeId.Trim();
        var sanitized = InvalidScopeIdChars().Replace(trimmed, "-");
        sanitized = CollapseDashes().Replace(sanitized, "-").Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static string Truncate(string key) =>
        key.Length <= OperationalDiagnosticsCacheKeyConstants.MaxKeyLength
            ? key
            : key[..OperationalDiagnosticsCacheKeyConstants.MaxKeyLength];

    [GeneratedRegex(@"[^a-zA-Z0-9._-]+")]
    private static partial Regex InvalidScopeIdChars();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex CollapseDashes();
}
