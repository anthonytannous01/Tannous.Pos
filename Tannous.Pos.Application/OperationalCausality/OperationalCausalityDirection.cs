namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Operator-facing causal movement direction.</summary>
public enum OperationalCausalityDirection
{
    Expanding = 0,
    Stabilizing = 1,
    Collapsing = 2,
    Cyclical = 3,
    Recurring = 4,
    Stable = 5
}
