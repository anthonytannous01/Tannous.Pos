namespace Tannous.Pos.Application.Audit;

/// <summary>Heuristic alert severity for operator diagnostics (query-time only; not delivered externally).</summary>
public static class OperationalAlertSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}
