namespace Tannous.Pos.Domain.Enums;

/// <summary>Operator-driven reconciliation workflow status (internal diagnostics only).</summary>
public enum ReconciliationResolutionStatus
{
    Unresolved = 0,
    Acknowledged = 1,
    Investigating = 2,
    Resolved = 3,
    Ignored = 4
}
