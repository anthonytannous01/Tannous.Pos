namespace Tannous.Pos.Application.Audit;

/// <summary>Operator-facing aging severity for unresolved sync conflicts (guidance only).</summary>
public static class OperationalConflictAgingSeverity
{
    public const string None = "None";
    public const string Advisory = "Advisory";
    public const string Elevated = "Elevated";
    public const string Critical = "Critical";
}
