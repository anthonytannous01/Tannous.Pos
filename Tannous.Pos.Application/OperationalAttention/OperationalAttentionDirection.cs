namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Direction of operational attention shift across bounded continuity.</summary>
public enum OperationalAttentionDirection
{
    EscalationFocused,
    StabilizationFocused,
    InvestigationFocused,
    ContainmentFocused,
    Balanced
}
