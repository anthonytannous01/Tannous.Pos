namespace Tannous.Pos.Application.Audit;

/// <summary>Allowed diagnostics cache scopes (bounded cardinality; no user/session scopes).</summary>
public static class OperationalDiagnosticsCacheScopes
{
    public const string Global = "global";
    public const string Device = "device";
    public const string Operation = "operation";
    public const string Order = "order";
    public const string Category = "category";
}
