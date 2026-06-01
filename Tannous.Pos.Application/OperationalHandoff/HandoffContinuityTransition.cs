namespace Tannous.Pos.Application.OperationalHandoff;

/// <summary>State continuity classification across the bounded snapshot window.</summary>
public enum HandoffContinuityTransition
{
    /// <summary>Fewer than 2 snapshots available — cannot assess continuity.</summary>
    Insufficient,

    /// <summary>State unchanged between first and most recent snapshot in the bounded window.</summary>
    Consistent,

    /// <summary>State changed between first and most recent snapshot in the bounded window.</summary>
    Shifted
}
