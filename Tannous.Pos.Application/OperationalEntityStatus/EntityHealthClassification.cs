namespace Tannous.Pos.Application.OperationalEntityStatus;

/// <summary>
/// Deterministic operational health classification for an order or device entity.
/// Based on audit severity and unresolved conflict count only — not business logic.
/// </summary>
public enum EntityHealthClassification
{
    /// <summary>No audit records found — entity not yet observed in operational audit trail.</summary>
    Unknown,

    /// <summary>Records present, Information-only severity, no unresolved conflicts.</summary>
    Healthy,

    /// <summary>Warning severity OR exactly 1 unresolved conflict (not both).</summary>
    Watchable,

    /// <summary>Warning severity AND at least 1 conflict, OR 2 unresolved conflicts.</summary>
    AtRisk,

    /// <summary>Critical severity present OR 3 or more unresolved conflicts.</summary>
    Critical
}
