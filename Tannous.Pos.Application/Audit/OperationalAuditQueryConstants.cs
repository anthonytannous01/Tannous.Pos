namespace Tannous.Pos.Application.Audit;

/// <summary>Operational audit diagnostics query limits (internal admin surface only).</summary>
public static class OperationalAuditQueryConstants
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
    public const int MinPage = 1;
}
