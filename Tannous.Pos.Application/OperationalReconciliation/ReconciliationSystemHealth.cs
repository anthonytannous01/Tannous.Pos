namespace Tannous.Pos.Application.OperationalReconciliation;

/// <summary>Deterministic classification of reconciliation system load state.</summary>
public enum ReconciliationSystemHealth
{
    /// <summary>No unresolved conflicts or fewer than 3 with no aged backlog.</summary>
    Stable,
    /// <summary>3–9 unresolved conflicts, or oldest conflict older than 1 hour.</summary>
    Pressured,
    /// <summary>10–19 unresolved conflicts, or oldest conflict older than 4 hours.</summary>
    Backlogged,
    /// <summary>20 or more unresolved conflicts, or oldest conflict older than 24 hours.</summary>
    Critical
}
